using Microsoft.EntityFrameworkCore;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using VectorDatabases.Data;

namespace VectorDatabases;

class QdrantMovieRecommender
{
    //https://qdrant.tech/documentation/advanced-tutorials/collaborative-filtering/
    //https://github.com/qdrant/examples/blob/master/collaborative-filtering/collaborative-filtering.ipynb

    readonly MovieContext dbContext;

    //https://qdrant.tech/documentation/examples/recommendation-system-ovhcloud/
    readonly Dictionary<int, List<(int, float)>> sparseVectors = new();

    public QdrantMovieRecommender()
    {
        var assemblyLocation = AppContext.BaseDirectory;
        var usersPath = Path.Combine(assemblyLocation, "Data", "ml-1m", "users.dat");
        var moviesPath = Path.Combine(assemblyLocation, "Data", "ml-1m", "movies.dat");
        var ratingsPath = Path.Combine(assemblyLocation, "Data", "ml-1m", "ratings.dat");

        dbContext = new MovieContext();
        this.loadData(usersPath, moviesPath, ratingsPath);
    }

    void loadData(string usersPath, string moviesPath, string ratingsPath)
    {
        Console.WriteLine("Loading users...");
        using var usersReader = new StreamReader(usersPath);
        while (!usersReader.EndOfStream)
        {
            var line = usersReader.ReadLine();
            var values = line?.Split("::");
            if (values == null) continue;

            dbContext.Users.Add(new User
            {
                UserId = int.Parse(values[0]),
                Gender = values[1],
                Age = int.Parse(values[2]),
                Occupation = values[3],
                Zip = values[4]
            });
        }

        Console.WriteLine("Loading movies...");
        using var moviesReader = new StreamReader(moviesPath);
        while (!moviesReader.EndOfStream)
        {
            var line = moviesReader.ReadLine();
            var values = line?.Split("::");
            if (values == null) continue;

            dbContext.Movies.Add(new Movie
            {
                MovieId = int.Parse(values[0]),
                Title = values[1],
                Genres = values[2]
            });
        }

        dbContext.SaveChanges();

        Console.WriteLine("Loading ratings...");
        var ratings = new List<Rating>();
        using var ratingsReader = new StreamReader(ratingsPath);
        while (!ratingsReader.EndOfStream)
        {
            var line = ratingsReader.ReadLine();
            var values = line?.Split("::");
            if (values == null) continue;

            ratings.Add(new Rating
            {
                UserId = int.Parse(values[0]),
                MovieId = int.Parse(values[1]),
                RatingValue = float.Parse(values[2]),
                Timestamp = long.Parse(values[3])
            });
        }

        /*
         * Comparability – Different rating sources or users may have different rating scales (e.g., some may rate harshly, others leniently)
         * Machine Learning – Many ML algorithms work better when input data has zero mean and unit variance (standardization). This helps avoid biases in training
         * Reducing Bias – Some users may consistently give higher or lower ratings than others. Normalization helps adjust for individual tendencies
         * Stability in Aggregation – If ratings come from multiple sources, normalization ensures fair weighting when aggregating scores.
         */
        Console.WriteLine("Normalizing ratings...");
        var mean = ratings.Average(r => r.RatingValue);
        var std = (float)Math.Sqrt(ratings.Average(r => Math.Pow(r.RatingValue - mean, 2)));

        foreach (var rating in ratings)
            rating.RatingValue = (rating.RatingValue - mean) / std;

        dbContext.Ratings.AddRange(ratings);
        dbContext.SaveChanges();

        Console.WriteLine("Aggregating...");
        //var mergedData = dbContext.Ratings
        //    .Include(r => r.Movie)
        //    .Select(r => new
        //    {
        //        r.RatingId, r.UserId, r.MovieId,
        //        r.RatingValue, r.Timestamp,
        //        MovieTitle = r.Movie.Title
        //    })
        //    .ToList();

        var ratingsAggregated = dbContext.Ratings
            .GroupBy(r => new { r.UserId, r.MovieId })
            .Select(g => new
            {
                g.Key.UserId, g.Key.MovieId,
                AverageRating = g.Average(r => r.RatingValue)
            })
            .ToList();

        Console.WriteLine("Creating sparse vectors...");
        foreach (var rating in ratingsAggregated)
        {
            if (!sparseVectors.ContainsKey(rating.UserId))
                sparseVectors[rating.UserId] = new();

            sparseVectors[rating.UserId].Add(new (rating.MovieId, rating.AverageRating));
        }
    }

    async Task uploadPointsAsync(QdrantClient client, string collectionName)
    {
        Console.WriteLine("Uploading points....");
        IEnumerable<(float, uint)> getValues(int id)
        {
            foreach (var item in sparseVectors[id])
                yield return new(item.Item2, (uint)item.Item1);
        }

        var users = await dbContext.Users.ToListAsync();
        var points = users.Select(user => new PointStruct
        {
            Id = (uint)user.UserId,
            Vectors = ("ratings", new Vector(           //float[], (float[], uint[]), (float, uint)[] or float[][]
                getValues(user.UserId).ToArray())),
            Payload =
            {
                ["user_id"] = user.UserId,
                ["gender"] = user.Gender,
                ["age"] = user.Age,
                ["occupation"] = user.Occupation,
                ["zip"] = user.Zip
            }
        });

        await client.UpsertAsync(collectionName, points: points.ToArray());
    }

    public async Task<List<Movie>> SearchMoviesByTitle(string titlePattern)
    {
        return await dbContext.Movies
            .Where(m => m.Title.Contains(titlePattern))
            .ToListAsync();
    }

    public async Task CreateCollection(string hostQdrant, string collectionName)
    {
        var client = new QdrantClient(hostQdrant, 6334); // https: false);

        //https://qdrant.tech/articles/sparse-vectors/
        //var sparseVectorConfig = new SparseVectorConfig();
        //sparseVectorConfig.Map["ratings"] = new SparseVectorParams();

        var vectorDimension = this.dbContext.Movies.LongCount();
        
        await client.RecreateCollectionAsync(collectionName,
            vectorsConfig: new VectorParams() { Distance = Distance.Cosine, Size = (ulong)vectorDimension },
            sparseVectorsConfig: new SparseVectorConfig(("ratings", new SparseVectorParams
            {
                Index = new SparseIndexConfig
                {
                    OnDisk = false, FullScanThreshold = 10000
                }
            })));
        await this.uploadPointsAsync(client, collectionName);
    }

    public async Task<IDictionary<string, float>> GetRecommendations(string hostQdrant, string collectionName, IDictionary<uint, float> givenRatings)
    {
        var client = new QdrantClient(hostQdrant);

        var searchResults = await client.SearchAsync(collectionName,
            vector: givenRatings.Values.ToArray(), sparseIndices: givenRatings.Keys.Select(k => (uint)k).ToArray(),
            vectorsSelector: new WithVectorsSelector { Include = new VectorsSelector { Names = { "ratings" } } },
            vectorName: "ratings", limit: 20);

        var movieScores = new Dictionary<uint, float>();
        foreach (var searchPoint in searchResults)
        {
            if (null != searchPoint && null != searchPoint.Vectors)
            {
                var ratings = searchPoint.Vectors.Vectors_.Vectors.GetValueOrDefault("ratings");
                if (null != ratings)
                {
                    int i = 0;
                    foreach (var index in ratings.Indices.Data)
                    {
                        if (!givenRatings.ContainsKey(index)) // we dont want already watched/ranked movies from givenRatings
                        {
                            if (!movieScores.ContainsKey(index)) movieScores.Add(index, 0);
                            movieScores[index] += ratings.Data[i];
                        }

                        i++;
                    }
                }
            }
        }

        //var preferredGenres = dbContext.Movies.Where(m => givenRatings.Keys.Contains((uint)m.MovieId))
        //    .ToArray() // the split below will not be translated to In memory "SQL"
        //    .SelectMany(m => m.Genres.Split("|".ToCharArray()))
        //    .Distinct();

        var movies = new Dictionary<string, float>();
        foreach(var key in movieScores.Keys)
        {
            var movie = dbContext.Movies.FirstOrDefault(m => m.MovieId == key);
            if (null != movie)
            {
                // lets keep ourselves restricted to watched genres
                //if (movie.Genres.Split("|".ToCharArray()).Any(g => preferredGenres.Contains(g)))
                {
                    if (!movies.ContainsKey(movie.Title)) movies.Add(movie.Title, 0);
                    movies[movie.Title] += movieScores[key];
                }
            }
        }

        return movies;
    }
}
