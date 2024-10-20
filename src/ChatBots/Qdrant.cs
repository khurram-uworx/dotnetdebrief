#pragma warning disable SKEXP0020

using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
//using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ChatBots;

//https://devblogs.microsoft.com/dotnet/introducing-microsoft-extensions-ai-preview/

class Movie
{
    [VectorStoreRecordKey]
    public int Key { get; set; }

    [VectorStoreRecordData]
    public string Title { get; set; }

    [VectorStoreRecordData]
    public string Description { get; set; }

    [VectorStoreRecordVector(384, DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Vector { get; set; }
}

internal class Qdrant
{
    public static async Task QuickStartAsync()
    {
        // https://qdrant.tech/documentation/quickstart

        var client = new QdrantClient("localhost", 6334); // gprc port; the other one is 6333
        var vectorStore = new QdrantVectorStore(client,
            options: new() { HasNamedVectors = true });

        if (!await client.CollectionExistsAsync("test_collection"))
            await client.CreateCollectionAsync("test_collection",
                vectorsConfig: new VectorParams() { Size = 4, Distance = Distance.Dot });

        var operationInfo = await client.UpsertAsync(collectionName: "test_collection",
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

        var searchResult1 = await client.QueryAsync("test_collection",
            query: new float[] { 0.2f, 0.1f, 0.9f, 0.7f },
            limit: 3);
        Console.WriteLine("# We should get Berlin, Moscow and then London");
        Console.WriteLine(searchResult1);
        Console.WriteLine(Environment.NewLine + " ========== " + Environment.NewLine);

        var searchResult2 = await client.QueryAsync("test_collection",
            query: new float[] { 0.2f, 0.1f, 0.9f, 0.7f },
            filter: Conditions.MatchKeyword("city", "London"),
            limit: 3, payloadSelector: true);

        Console.WriteLine("# Even though it is least similar; but due to filtering we will get London");
        Console.WriteLine(searchResult2);
        Console.WriteLine(Environment.NewLine + " ========== " + Environment.NewLine);
    }

    public static async Task VectorDataExtensionsAsync()
    {
        //https://devblogs.microsoft.com/dotnet/introducing-microsoft-extensions-vector-data
        //https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/out-of-the-box-connectors/qdrant-connector

        var movieData = new List<Movie>()
        {
            new Movie
            {
                Key = 0,
                Title = "Lion King",
                Description = "The Lion King is a classic Disney animated film that tells the story of a young lion named Simba who embarks on a journey to reclaim his throne as the king of the Pride Lands after the tragic death of his father."
            },
            new Movie
            {
                Key = 1,
                Title = "Inception",
                Description = "Inception is a science fiction film directed by Christopher Nolan that follows a group of thieves who enter the dreams of their targets to steal information."
            },
            new Movie
            {
                Key = 2,
                Title = "The Matrix",
                Description = "The Matrix is a science fiction film directed by the Wachowskis that follows a computer hacker named Neo who discovers that the world he lives in is a simulated reality created by machines."
            },
            new Movie
            {
                Key = 3,
                Title = "Shrek",
                Description = "Shrek is an animated film that tells the story of an ogre named Shrek who embarks on a quest to rescue Princess Fiona from a dragon and bring her back to the kingdom of Duloc."
            }
        };

        //var vectorStore = new InMemoryVectorStore();
        var vectorStore = new QdrantVectorStore(new QdrantClient("localhost"));

        //var collection = new QdrantVectorStoreRecordCollection<Movie>(
        //    new QdrantClient("localhost"),
        //    collectionName: "movies",
        //    options: new() { HasNamedVectors = true });

        var movies = vectorStore.GetCollection<int, Movie>("movies");
        await movies.CreateCollectionIfNotExistsAsync();

        IEmbeddingGenerator<string, Embedding<float>> generator = new OllamaEmbeddingGenerator(
            new Uri("http://localhost:11434/"), "all-minilm");

        foreach (var movie in movieData)
        {
            movie.Vector = await generator.GenerateEmbeddingVectorAsync(movie.Description);
            //await collection.UpsertAsync(new Hotel
            //{
            //    HotelId = 1,
            //    HotelName = "Hotel Happy",
            //    Description = "A place where everyone can be happy.",
            //    DescriptionEmbedding = new float[4] { 0.9f, 0.1f, 0.1f, 0.1f }
            //});

            await movies.UpsertAsync(movie);
        }

        var query = "A family friendly movie";
        var queryEmbedding = await generator.GenerateEmbeddingVectorAsync(query);

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
