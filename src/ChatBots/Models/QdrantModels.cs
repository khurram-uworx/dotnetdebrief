#pragma warning disable SKEXP0001

using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;
using System;

namespace ChatBots.Models;

//https://github.com/microsoft/semantic-kernel/blob/926a59095b8b9a4a2f0b9cf616caf9a1045b360f/dotnet/samples/Demos/OnnxSimpleRAG/Program.cs
class Movie
{
    [TextSearchResultLink] // we can alternatively have "mapper" in VectorTextSearch
    [VectorStoreRecordKey]
    public ulong Key { get; set; } // keys can only be ulong or Guid (for Qdrant?)

    [TextSearchResultName]
    [VectorStoreRecordData]
    public string Title { get; set; }

    [TextSearchResultValue]
    [VectorStoreRecordData]
    public string Description { get; set; }

    [VectorStoreRecordVector(384, DistanceFunction.CosineSimilarity)]
    public ReadOnlyMemory<float> Vector { get; set; }
}
