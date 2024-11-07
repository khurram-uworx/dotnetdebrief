namespace VectorDatabases;

internal class Program
{
    static async Task Main(string[] args)
    {
        var q = new QdrantMovieRecommender();
        var list = await q.SearchMoviesByTitle("Tom");

        foreach (var m in list)
            Console.WriteLine($"{m.MovieId}: {m.Title}");
    }
}
