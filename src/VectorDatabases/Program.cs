using VectorDatabases;

var q = new QdrantMovieRecommender(); // will create in memory database from ml-1m

var list = await q.SearchMoviesByTitle("Redemption");
foreach (var m in list)
    Console.WriteLine($"{m.MovieId}: {m.Title}");

Console.WriteLine("Ensure qdran is running; docker run --rm -p 6333:6333 -p 6334:6334 qdrant/qdrant, press Enter to continue");
Console.ReadLine();

var hostQdrant = "localhost";
await q.CreateCollection(hostQdrant, collectionName: "ml-1m");
var recommendations = await q.GetRecommendations(hostQdrant, collectionName: "ml-1m",
    givenRatings: new Dictionary<uint, float> {
        [1] = 1f,       // Toy Story (1995)
        [13] = 1f,      // Balto (1995)
        [48] = 1f,      // Pocahontas (1995)
        [239] = 1f,     // Goofy Movie, A (1995)
        [244] = 1f      // Gumby: The Movie (1995)
    });

foreach(var recommendation in recommendations.OrderByDescending(s => s.Value).Take(3))
    Console.WriteLine($"{recommendation.Key} [{recommendation.Value}]");
