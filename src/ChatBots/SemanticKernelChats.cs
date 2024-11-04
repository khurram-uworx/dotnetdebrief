#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0050

using ChatBots.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Embeddings;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Plugins.Memory;
using OpenAI;
using System;
using System.ClientModel;
using System.Threading.Tasks;

namespace ChatBots;

internal static class SemanticKernelChats
{
    public static Kernel GetKernel(Action<IKernelBuilder, OpenAIClient> action = null)
    {
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri("http://127.0.0.1:11434/v1"), // Ollama
            // Endpoint = new Uri("http://127.0.0.1:5272/v1") // AI Toolkit
            NetworkTimeout = TimeSpan.FromMinutes(5)
        };
        var openAIClient = new OpenAIClient(new ApiKeyCredential("x"), options);

        // Create a chat completion service
        var builder = Kernel.CreateBuilder();
        builder.AddOpenAIChatCompletion(
            modelId: "mistral", // llama2 and phi3 dont support tools
            openAIClient);

        // Add enterprise components
        builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Warning));

        if (null != action) action(builder, openAIClient);

        return builder.Build();
    }

    public static async Task HelloWorldAsync()
    {
        var kernel = GetKernel();

        // Example 1. Invoke the kernel with a prompt and display the result
        Console.WriteLine(await kernel.InvokePromptAsync("What color is the sky?"));
        Console.WriteLine();

        // Example 2. Invoke the kernel with a templated prompt and display the result
        KernelArguments arguments = new() { { "topic", "sea" } };
        Console.WriteLine(await kernel.InvokePromptAsync("What color is the {{$topic}}?", arguments));
        Console.WriteLine();

        // Example 3. Invoke the kernel with a templated prompt and stream the results to the display
        await foreach (var update in kernel.InvokePromptStreamingAsync("What color is the {{$topic}}? Provide a detailed explanation.", arguments))
        {
            Console.Write(update);
        }

        return; // the follow scenarios are too demanding for CPU

        Console.WriteLine(string.Empty);

        // Example 4. Invoke the kernel with a templated prompt and execution settings
        arguments = new(new OpenAIPromptExecutionSettings { MaxTokens = 500, Temperature = 0.5 }) { { "topic", "dogs" } };
        Console.WriteLine(await kernel.InvokePromptAsync("Tell me a story about {{$topic}}", arguments));

        // Example 5. Invoke the kernel with a templated prompt and execution settings configured to return JSON
#pragma warning disable SKEXP0010
        arguments = new(new OpenAIPromptExecutionSettings { ResponseFormat = "json_object" }) { { "topic", "chocolate" } };
        Console.WriteLine(await kernel.InvokePromptAsync("Create a recipe for a {{$topic}} cake in JSON format", arguments));
    }

    public static async Task PromptScenarioAsync()
    {
        // https://github.com/microsoft/semantic-kernel/blob/main/dotnet/README.md

        var kernel = GetKernel();

        string translationPrompt = @"{{$input}}
Translate the text to math.";
        string summarizePrompt = @"{{$input}}
Give me a TLDR with the fewest words.";

        var translator = kernel.CreateFunctionFromPrompt(translationPrompt); //executionSettings: new OpenAIPromptExecutionSettings { MaxTokens = 100 });
        var summarize = kernel.CreateFunctionFromPrompt(summarizePrompt);

        string inputThermodynamics = @"
1st Law of Thermodynamics - Energy cannot be created or destroyed.
2nd Law of Thermodynamics - For a spontaneous process, the entropy of the universe increases.
3rd Law of Thermodynamics - A perfect crystal at zero Kelvin has zero entropy.";

        string textNewton = @"
1. An object at rest remains at rest, and an object in motion remains in motion at constant speed and in a straight line unless acted on by an unbalanced force.
2. The acceleration of an object depends on the mass of the object and the amount of force applied.
3. Whenever one object exerts a force on another object, the second object exerts an equal and opposite on the first.";

        //Console.WriteLine(await kernel.InvokeAsync(summarize, new() { ["input"] = inputThermodynamics }));
        //Console.WriteLine(await kernel.InvokeAsync(summarize, new() { ["input"] = textNewton }));

        var summary = await kernel.InvokeAsync(summarize, new() { ["input"] = inputThermodynamics });
        Console.WriteLine(await kernel.InvokeAsync(translator, new() { ["input"] = summary.ToString() }));
    }

    public static async Task LightsPluginAsync()
    {
        //https://learn.microsoft.com/en-us/semantic-kernel/get-started/quick-start-guide

        var kernel = GetKernel();

        // Retrieve the chat completion service
        var chatCompletionService = kernel.Services.GetRequiredService<IChatCompletionService>();

        kernel.Plugins.AddFromType<LightsPlugin>("Lights");

        OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
        {
            //ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions, // this cant be used together with FunctionChoiceBehavior
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        // Create chat history
        var history = new ChatHistory();

        // Get the response from the AI
        //var result = await chatCompletionService.GetChatMessageContentAsync(
        //    history,
        //    executionSettings: openAIPromptExecutionSettings,
        //    kernel: kernel
        //);

        string? userInput;
        do
        {
            // Collect user input
            Console.Write("User> ");
            userInput = Console.ReadLine();

            // Add user input
            history.AddUserMessage(userInput);

            // Get the response from the AI
            var result = await chatCompletionService.GetChatMessageContentAsync(
                history,
                executionSettings: openAIPromptExecutionSettings,
                kernel: kernel);

            // Print the results
            Console.WriteLine("Assistant> " + result);

            // Add the message from the agent to the chat history
            history.AddMessage(result.Role, result.Content ?? string.Empty);
        } while (userInput is not null);
    }

    public static async Task RagScenarioAsync()
    {
        //https://elbruno.com/2024/06/17/full-rag-scenario-using-phi3-semantickernel-and-textmemory-in-local-mode/
        //https://techcommunity.microsoft.com/t5/educator-developer-blog/building-intelligent-applications-with-local-rag-in-net-and-phi/ba-p/4175721

        var kernel = GetKernel((b, _) => b.AddLocalTextEmbeddingGeneration());

        var question = "What is Bruno's favourite super hero?";
        Console.WriteLine($"This program will answer the following question: {question}");
        Console.WriteLine("1st approach will be to ask the question directly to the AI model.");
        Console.WriteLine("2nd approach will be to add facts to a semantic memory and ask the question again");
        Console.WriteLine("");

        Console.WriteLine($"AI response (no memory).");
        var response = kernel.InvokePromptStreamingAsync(question);
        await foreach (var result in response)
        {
            Console.Write(result);
        }

        // separator
        Console.WriteLine("");
        Console.WriteLine("==============");
        Console.WriteLine("");

        // get the embeddings generator service
        var embeddingGenerator = kernel.Services.GetRequiredService<ITextEmbeddingGenerationService>();
        var memory = new SemanticTextMemory(new VolatileMemoryStore(), embeddingGenerator);

        // add facts to the collection
        const string MemoryCollectionName = "fanFacts";

        await memory.SaveInformationAsync(MemoryCollectionName, id: "info1", text: "Gisela's favourite super hero is Batman");
        await memory.SaveInformationAsync(MemoryCollectionName, id: "info2", text: "The last super hero movie watched by Gisela was Guardians of the Galaxy Vol 3");
        await memory.SaveInformationAsync(MemoryCollectionName, id: "info3", text: "Bruno's favourite super hero is Invincible");
        await memory.SaveInformationAsync(MemoryCollectionName, id: "info4", text: "The last super hero movie watched by Bruno was Aquaman II");
        await memory.SaveInformationAsync(MemoryCollectionName, id: "info5", text: "Bruno don't like the super hero movie: Eternals");

        TextMemoryPlugin memoryPlugin = new(memory);

        // Import the text memory plugin into the Kernel.
        kernel.ImportPluginFromObject(memoryPlugin);

        OpenAIPromptExecutionSettings settings = new()
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
        };

        var prompt = @"
Question: {{$input}}
Answer the question using the memory content: {{Recall}}";

        var arguments = new KernelArguments(settings)
        {
            { "input", question },
            { "collection", MemoryCollectionName }
        };

        Console.WriteLine($"AI response (using semantic memory).");

        // https://github.com/microsoft/semantic-kernel/issues/5327
        //response = kernel.InvokePromptStreamingAsync(prompt, arguments);
        //await foreach (var result in response)
        //    Console.Write(result);

        // https://github.com/ollama/ollama/issues/5990 Tools and properties.type Not Supporting Arrays
        var resultFunction = await kernel.InvokePromptAsync(prompt, arguments);

        Console.WriteLine(resultFunction);
    }
}
