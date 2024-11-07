using Microsoft.EntityFrameworkCore;
using VectorDatabases.Data;

namespace VectorDatabases;

internal class QdrantMovieRecommender
{
    readonly MovieContext dbContext;

    //https://qdrant.tech/documentation/examples/recommendation-system-ovhcloud/
    readonly Dictionary<int, (List<float> values, List<int> indices)> userSparseVectors = new();

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
        // Load users
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

        // Load movies
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

        // Load ratings
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

        // Normalize ratings
        var mean = ratings.Average(r => r.RatingValue);
        var std = (float)Math.Sqrt(ratings.Average(r => Math.Pow(r.RatingValue - mean, 2)));
        foreach (var rating in ratings)
            rating.RatingValue = (rating.RatingValue - mean) / std;

        dbContext.Ratings.AddRange(ratings);
        dbContext.SaveChanges();

        // Create sparse vectors
        foreach (var rating in ratings)
        {
            if (!userSparseVectors.ContainsKey(rating.UserId))
                userSparseVectors[rating.UserId] = (new List<float>(), new List<int>());

            userSparseVectors[rating.UserId].values.Add(rating.RatingValue);
            userSparseVectors[rating.UserId].indices.Add(rating.MovieId);
        }
    }

    public async Task<List<Movie>> SearchMoviesByTitle(string titlePattern)
    {
        return await dbContext.Movies
            .Where(m => m.Title.Contains(titlePattern))
            .ToListAsync();
    }
}
