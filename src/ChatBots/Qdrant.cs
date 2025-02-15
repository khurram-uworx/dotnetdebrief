using ChatBots.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Models;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChatBots;

//https://devblogs.microsoft.com/dotnet/introducing-microsoft-extensions-ai-preview/

static class Qdrant
{
    public static async Task<IVectorStoreRecordCollection<ulong, Movie>> CreateMovieCollectionAsync(
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        IVectorStore vectorStore, string collectionName)
    {
        var movies = vectorStore.GetCollection<ulong, Movie>(collectionName); // keys can only be ulong or Guid (for Qdrant?)
        await movies.CreateCollectionIfNotExistsAsync();

        foreach (var movie in TestData.GetMovies())
        {
            movie.Vector = await embeddingGenerator.GenerateEmbeddingVectorAsync(movie.Description);
            await movies.UpsertAsync(movie);
        }

        return movies;
    }

    public static async Task QuickStartAsync(string hostQdrant, string collectionName)
    {
        // https://qdrant.tech/documentation/quickstart

        var client = new QdrantClient(hostQdrant, 6334); // gprc port; the other one is 6333
        //var vectorStore = new QdrantVectorStore(client,
        //    options: new() { HasNamedVectors = true });

        if (!await client.CollectionExistsAsync(collectionName))
            await client.CreateCollectionAsync(collectionName,
                vectorsConfig: new VectorParams() { Size = 4, Distance = Distance.Dot });

        var operationInfo = await client.UpsertAsync(collectionName,
            points: new List<PointStruct>
            {
                new()
                {
                    Id = 1,
                    Vectors = new float[] { 0.05f, 0.61f, 0.76f, 0.74f },
                    Payload = { ["city"] = "Berlin" }
                },
                new()
                {
                    Id = 2,
                    Vectors = new float[] { 0.19f, 0.81f, 0.75f, 0.11f },
                    Payload = { ["city"] = "London" }
                },
                new()
                {
                    Id = 3,
                    Vectors = new float[] { 0.36f, 0.55f, 0.47f, 0.94f },
                    Payload = { ["city"] = "Moscow" }
                }
            });

        var searchResult1 = await client.QueryAsync(collectionName,
            query: new float[] { 0.2f, 0.1f, 0.9f, 0.7f },
            limit: 3);

        Console.WriteLine("# We should get Berlin, Moscow and then London");
        Console.WriteLine(searchResult1);
        Console.WriteLine(Environment.NewLine + " ========== " + Environment.NewLine);

        var searchResult2 = await client.QueryAsync(collectionName,
            query: new float[] { 0.2f, 0.1f, 0.9f, 0.7f },
            filter: Conditions.MatchKeyword("city", "London"),
            limit: 3, payloadSelector: true);

        Console.WriteLine("# Even though it is least similar; but due to filtering we will get London");
        Console.WriteLine(searchResult2);
        Console.WriteLine(Environment.NewLine + " ========== " + Environment.NewLine);
    }

    public static async Task VectorDataExtensionsAsync(string urlOllama, string embeddingModelName, string hostQdrant, string collectionName)
    {
        //https://devblogs.microsoft.com/dotnet/introducing-microsoft-extensions-vector-data
        //https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/out-of-the-box-connectors/qdrant-connector

        //var vectorStore = new InMemoryVectorStore();
        IVectorStore vectorStore = new QdrantVectorStore(new QdrantClient(hostQdrant));
        //var collection = new QdrantVectorStoreRecordCollection<Movie>(
        //    new QdrantClient(hostQdrant),
        //    collectionName: "movies",
        //    options: new() { HasNamedVectors = true });
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = new OllamaEmbeddingGenerator(
            new Uri(urlOllama), embeddingModelName);
        var movies = await CreateMovieCollectionAsync(embeddingGenerator, vectorStore, collectionName);

        var query = "A family friendly movie";
        var queryEmbedding = await embeddingGenerator.GenerateEmbeddingVectorAsync(query);

        var searchOptions = new VectorSearchOptions()
        {
            Top = 1,
            VectorPropertyName = "Vector"
        };

        var results = await movies.VectorizedSearchAsync(queryEmbedding, searchOptions);

        await foreach (var result in results.Results)
        {
            Console.WriteLine($"Title: {result.Record.Title}");
            Console.WriteLine($"Description: {result.Record.Description}");
            Console.WriteLine($"Score: {result.Score}");
            Console.WriteLine();
        }
    }
}
