namespace VectorDatabases;

internal class Program
{
    static async Task Main(string[] args)
    {
        //docker run --rm -p 6333:6333 -p 6334:6334 qdrant/qdrant
        var q = new QdrantMovieRecommender(); // will create in memory database from ml-1m

        var list = await q.SearchMoviesByTitle("Redemption");
        foreach (var m in list)
            Console.WriteLine($"{m.MovieId}: {m.Title}");

        await q.CreateCollection(collectionName: "ml-1m");
        var recommendations = await q.GetRecommendations(collectionName: "ml-1m",
            givenRatings: new Dictionary<uint, float> {
                [1] = 1f,       // Toy Story (1995)
                [13] = 1f,      // Balto (1995)
                [48] = 1f,      // Pocahontas (1995)
                [239] = 1f,     // Goofy Movie, A (1995)
                [244] = 1f      // Gumby: The Movie (1995)
            });

        foreach(var recommendation in recommendations.OrderByDescending(s => s.Value).Take(5))
            Console.WriteLine($"{recommendation.Key} [{recommendation.Value}]");
    }
}
