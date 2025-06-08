using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;
using System;

namespace ChatBots.Models;

//https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/defining-your-data-model?pivots=programming-language-csharp
//https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/Concepts/Memory/VectorStore_DataIngestion_MultiStore.cs
//https://github.com/microsoft/semantic-kernel/blob/926a59095b8b9a4a2f0b9cf616caf9a1045b360f/dotnet/samples/Demos/OnnxSimpleRAG/Program.cs
class Movie
{
    [TextSearchResultLink] // we can alternatively have "mapper" in VectorTextSearch
    [VectorStoreKey]
    public ulong Key { get; set; } // keys can only be ulong or Guid (for Qdrant?)

    [TextSearchResultName]
    [VectorStoreData]
    public string Title { get; set; }

    [TextSearchResultValue]
    [VectorStoreData]
    public string Description { get; set; }

    [VectorStoreVector(Dimensions: 384, DistanceFunction = DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Vector { get; set; }
}
