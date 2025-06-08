#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0020
#pragma warning disable SKEXP0050

using ChatBots.Helpers;
using ChatBots.Models;
using ChatBots.Plugins;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Plugins.Memory;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using OllamaSharp;
using System;
using System.Threading.Tasks;

namespace ChatBots;

static class QdrantSemanticKernel
{
    /*
     * This is deprecated
    public static async Task RagClinicScenarioUsingSemanticMemoryAsync(string urlOllama, string textModel, string urlQdrant, string hostQdrant, string collectionName)
    {
        // https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/out-of-the-box-connectors/qdrant-connector
        // https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/code-samples
        // https://github.com/kinfey/SemanticKernelCookBook/blob/main/notebooks/dotNET/05/EmbeddingsWithSK.ipynb

        var kernel = SemanticKernelHelper.GetKernel(urlOllama, textModel,
            (b, c) =>
            {
                b.AddLocalTextEmbeddingGeneration();
                //b.AddLocalTextEmbeddingGeneration(modelName: "mxbai-embed-large");
                // if we specify any model; its onnx files needs to be in LocalEmbeddings folder
                
                //b.AddOpenAITextEmbeddingGeneration(modelId: "all-minilm", openAIClient: c);//, dimensions: 1536); //all-minilm, mxbai-embed-large, mistral
                b.Services.AddQdrantVectorStore(hostQdrant);
            });

        kernel.Plugins.AddFromType<ClinicPlugins>("Clinic");

        //ISemanticTextMemory memory = new MemoryBuilder()
        //.WithHttpClient(new HttpClient())
        //.WithLoggerFactory(kernel.LoggerFactory)
        //.WithOllamaTextEmbeddingGeneration("nomic-embed-text", "http://10.0.0.49:11434")
        //.WithMemoryStore(new ChromaMemoryStore("http://127.0.0.1:8000"))
        //.Build();

        ////var textEmbedding = new OpenAITextEmbeddingGenerationService(modelId: "mxbai-embed-large", apiKey: "x");
        //var qdrantMemoryBuilder = new MemoryBuilder();
        ////qdrantMemoryBuilder.WithTextEmbeddingGeneration(textEmbedding);
        ////qdrantMemoryBuilder.wit
        //qdrantMemoryBuilder.WithQdrantMemoryStore(urlQdrant, 1536);
        //var qdrantBuilder = qdrantMemoryBuilder.Build();


        // https://github.com/microsoft/semantic-kernel/issues/6483
        //ISemanticTextMemory memory = new MemoryBuilder()
        //    //.WithMemoryStore(memoryStore)
        //    .WithTextEmbeddingGeneration(embeddingGenerator)
        //    .WithQdrantMemoryStore(endpoint: urlQdrant, vectorSize: 1536)
        //    .Build();

        var embeddingGenerator = kernel.Services.GetRequiredService<ITextEmbeddingGenerationService>();
        var memory = new SemanticTextMemory(new QdrantMemoryStore(urlQdrant, 384), embeddingGenerator);

        //await memory.ImportTextAsync("Today is October 32nd, 2476");
        //// Generate an answer - This uses OpenAI for embeddings and finding relevant data
        //var answer = await memory.AskAsync("What's the current date (don't check for validity)?");
        //Console.WriteLine(answer.Question);
        //Console.WriteLine(answer.Result);

        await memory.SaveInformationAsync(collectionName,
            "Today is friday",
            id: "dayOfWeek");
        await memory.SaveInformationAsync(collectionName,
            "Clinic opens in morning from 10AM to 1PM, Mondays to Saturdays, Friday is off",
            id: "timing1");
        await memory.SaveInformationAsync(collectionName,
            "Clinic opens in evening from 6PM to 8PM, Mondays to Wednesdays only, for the rest of the week clinic is off in evening",
            id: "timing2");
        await memory.SaveInformationAsync(collectionName,
            "Clinic is off on Sunday",
            id: "timing3");

        var plugin = new TextMemoryPlugin(memory); // need ISemanticTextMemory here
        kernel.ImportPluginFromObject(plugin, "memory");

        OpenAIPromptExecutionSettings settings = new()
        {
            //ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions, // this cant be used together with FunctionChoiceBehavior
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var prompt = @"
                Question: {{$input}}
                Answer the question using the memory content: {{Recall}}";

        ////https://medium.com/@johnkane24/local-memory-c-semantic-kernel-ollama-and-sqlite-to-manage-chat-memories-locally-9b779fc56432
        var getArguments = new Func<string, KernelArguments>(
            q => new KernelArguments(settings)
            {
                { "input", q },
                { "collection", collectionName }
            });

        var question = "Will clinic open later in the day today?";
        var resultFunction = await kernel.InvokePromptAsync(prompt, getArguments(question));
        Console.WriteLine(resultFunction);
    }
    */

    //https://github.com/microsoft/semantic-kernel/blob/main/dotnet/samples/GettingStartedWithTextSearch/Step4_Search_With_VectorStore.cs
    public static async Task RagClinicScenarioUsingVectorSearchAsync(string urlOllama, string textModel, string embeddingModelName, string hostQdrant, string collectionName)
    {
        var kernel = SemanticKernelHelper.GetKernel(urlOllama, textModel,
            (b, c) =>
            {
                //b.AddOpenAITextEmbeddingGeneration(modelId: embeddingModelName, openAIClient: c);
                // facing https://github.com/microsoft/semantic-kernel/issues/8833
                //b.AddLocalTextEmbeddingGeneration();

                b.Services.AddQdrantVectorStore(hostQdrant); // this is more generalized that register IVectorStore
                // we can also use AddQdrantVectorStoreRecordCollection that will register IVectorStoreRecordCollection and IVectorizedSearch
                // https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/vector-search
                // IVectorStoreRecordCollection inherits from IVectorizedSearch
            });

        // https://learn.microsoft.com/en-us/semantic-kernel/concepts/text-search/out-of-the-box-textsearch/vectorstore-textsearch
        //var textEmbeddingGenerator = kernel.Services.GetRequiredService<ITextEmbeddingGenerationService>();
        IEmbeddingGenerator<string, Embedding<float>> textEmbeddingGenerator = new OllamaApiClient(
            new Uri(urlOllama), embeddingModelName);
        var vectorStore = kernel.Services.GetRequiredService<VectorStore>();

        VectorStoreCollection<ulong, Movie> collection = vectorStore.GetCollection<ulong, Movie>(collectionName); // keys can only be ulong or Guid (for Qdrant?)
        var embeddings = await textEmbeddingGenerator.GenerateAsync("A family friendly movie");
        //ReadOnlyMemory<float> searchVector = embeddings.Vector; //embeddings.Data;

        await Qdrant.CreateMovieCollectionAsync(textEmbeddingGenerator, vectorStore, collectionName);

        // https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/vector-search?pivots=programming-language-csharp
        // Do the search, passing an options object with a Top value to limit resulst to the single top match.
        var searchResult = collection.SearchAsync(embeddings.Vector, top: 1); //.VectorizedSearchAsync(searchVector, new() { Top = 1 });

        // Inspect the returned hotel.
        await foreach (var record in searchResult)
        {
            Console.WriteLine("Found record score: " + record.Score);
            Console.WriteLine("Found record key: " + record.Record.Key);
            Console.WriteLine("Found record title: " + record.Record.Title);
            Console.WriteLine("Found record description: " + record.Record.Description);
        }

        if (collection is IVectorSearchable<Movie> vectorizedSearch)
        {
            // https://learn.microsoft.com/en-us/semantic-kernel/concepts/text-search/text-search-vector-stores
            var textSearch = new VectorStoreTextSearch<Movie>(vectorizedSearch, textEmbeddingGenerator);//, new OllamaTextEmbeddingService(
                //textEmbeddingGenerator as OllamaEmbeddingGenerator));
            var searchPlugin = textSearch.CreateWithGetTextSearchResults("SearchPlugin");
            kernel.Plugins.Add(searchPlugin);

            var query = "On the weekend; family is getting together and we are thinking to watch a movie, can you recommend something?";
            string promptTemplate = """
                {{#with (SearchPlugin-GetTextSearchResults query)}}  
                  {{#each this}}  
                    Name: {{Name}}
                    Value: {{Value}}
                    Link: {{Link}}
                    -----------------
                  {{/each}}  
                {{/with}}  

                {{query}}

                Include citations to the relevant information where it is referenced in the response.
                """;
            // penAIPromptExecutionSettings settings = new() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };
            KernelArguments arguments = new() { { "query", query } }; // KernelArguments arguments = new(settings);
            HandlebarsPromptTemplateFactory promptTemplateFactory = new();
            Console.WriteLine(await kernel.InvokePromptAsync(
                promptTemplate,
                arguments,
                templateFormat: HandlebarsPromptTemplateFactory.HandlebarsTemplateFormat,
                promptTemplateFactory: promptTemplateFactory
            ));
        }
    }
}
