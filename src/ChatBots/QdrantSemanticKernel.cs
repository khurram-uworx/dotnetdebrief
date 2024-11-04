#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0020
#pragma warning disable SKEXP0050

using ChatBots.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Qdrant;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Plugins.Memory;
using System;
using System.Threading.Tasks;

namespace ChatBots;

internal class QdrantSemanticKernel
{
    public static async Task RagClinicScenarioAsync(string memoryCollectionName)
    {
        // https://learn.microsoft.com/en-us/semantic-kernel/concepts/vector-store-connectors/code-samples
        // https://github.com/kinfey/SemanticKernelCookBook/blob/main/notebooks/dotNET/05/EmbeddingsWithSK.ipynb

        var kernel = SemanticKernelChats.GetKernel(
            (b, c) =>
            {
                b.AddLocalTextEmbeddingGeneration();
                //b.AddLocalTextEmbeddingGeneration(modelName: "mxbai-embed-large");
                // if we specify any model; its onnx files needs to be in LocalEmbeddings folder
                
                //b.AddOpenAITextEmbeddingGeneration(modelId: "all-minilm", openAIClient: c);//, dimensions: 1536); //all-minilm, mxbai-embed-large, mistral
                b.AddQdrantVectorStore("localhost");
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
        //qdrantMemoryBuilder.WithQdrantMemoryStore("http://localhost:6333", 1536);
        //var qdrantBuilder = qdrantMemoryBuilder.Build();


        // https://github.com/microsoft/semantic-kernel/issues/6483
        //ISemanticTextMemory memory = new MemoryBuilder()
        //    //.WithMemoryStore(memoryStore)
        //    .WithTextEmbeddingGeneration(embeddingGenerator)
        //    .WithQdrantMemoryStore(endpoint: "http://localhost:6333", vectorSize: 1536)
        //    .Build();

        var embeddingGenerator = kernel.Services.GetRequiredService<ITextEmbeddingGenerationService>();
        var memory = new SemanticTextMemory(new QdrantMemoryStore("http://localhost:6333", 384), embeddingGenerator);

        //await memory.ImportTextAsync("Today is October 32nd, 2476");
        //// Generate an answer - This uses OpenAI for embeddings and finding relevant data, and LM Studio to generate an answer
        //var answer = await memory.AskAsync("What's the current date (don't check for validity)?");
        //Console.WriteLine(answer.Question);
        //Console.WriteLine(answer.Result);

        await memory.SaveInformationAsync(memoryCollectionName,
            "Today is friday",
            id: "dayOfWeek");
        await memory.SaveInformationAsync(memoryCollectionName,
            "Clinic opens in morning from 10AM to 1PM, Mondays to Saturdays, Friday is off",
            id: "timing1");
        await memory.SaveInformationAsync(memoryCollectionName,
            "Clinic opens in evening from 6PM to 8PM, Mondays to Wednesdays only, for the rest of the week clinic is off in evening",
            id: "timing2");
        await memory.SaveInformationAsync(memoryCollectionName,
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
                { "collection", memoryCollectionName }
            });

        var question = "Will clinic open later in the day today?";
        var resultFunction = await kernel.InvokePromptAsync(prompt, getArguments(question));
        Console.WriteLine(resultFunction);
    }
}
