using Microsoft.Extensions.AI;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace ChatBots;

static class AIExtensionTools
{
    class UmbrellaNeedResult
    {
        public string weather { get; set; }
        public bool needed { get; set; }
    }

    class CountryResult
    {
        public string name { get; set; }
        public string capital { get; set; }
        public string[] languages { get; set; }
    }

    public static async Task ChatWithAIExtensionAsync(string urlOllama, string textModel)
    {
        [Description("Gets the weather")]
        string GetWeather() => "It's raining";
        //Random.Shared.NextDouble() > 0.5
        //    ? "It's sunny"
        //    : "It's raining";

        // https://github.com/dotnet/extensions/tree/main/src/Libraries/Microsoft.Extensions.AI.Ollama
        IChatClient chatClient = new OllamaChatClient(new Uri(urlOllama), textModel)
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();

        //Console.WriteLine(await chatClient.CompleteAsync("Do I need an umbrella?",
        //    options: new ChatOptions
        //    {
        //        Tools = [AIFunctionFactory.Create(GetWeather)]
        //    }));

        //List<ChatMessage> conversationMessages =
        //[
        //    new ChatMessage(ChatRole.System, "You are a virtual assistant capable of handling various tasks through a series of tools..."),
        //    new ChatMessage(ChatRole.User, "Do I need an umbrella?")
        //];
        //ChatCompletion completion = await chatClient.CompleteAsync(conversationMessages, chatOptions);
        //Console.WriteLine(completion);

        // https://github.com/dotnet/extensions/blob/20c12ef61fc33865f36c1f4f6e8e2240e8c25f32/test/Libraries/Microsoft.Extensions.AI.Tests/ChatCompletion/ChatClientStructuredOutputExtensionsTests.cs
        //ChatResponseFormatJson json = new ChatResponseFormatJson(""""
        //    {
        //      "type": "object",
        //      "properties": {
        //        "name": { "type": "string" },
        //        "capital": { "type": "string" },
        //        "languages": { "type": "array", "items": { "type": "string" } }
        //      },
        //      "required": [ "name", "capital", "languages" ]
        //    }
        //    """", schemaName: "query_result", schemaDescription: "use this schema to answer the query");

        //ChatCompletion completion = await chatClient.CompleteAsync("Tell me about Canada",
        //    options: new ChatOptions
        //    {
        //        ResponseFormat = json // or ChatResponseFormat.Json; model will itself decide format
        //    });

        // https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/quickstart-local-ai
        // https://github.com/dotnet/ai-samples/blob/main/src/microsoft-extensions-ai/ollama/OllamaExamples/ToolCalling.cs
        // https://github.com/dotnet/extensions/pull/5730
        //var response = await chatClient.CompleteAsync<CountryResult>("Pick any country and give me its needed information");
        //Console.WriteLine($"{response.Result.name}, {response.Result.capital}");
        //if (response.Result.languages is not null)
        //    foreach (var lang in response.Result.languages)
        //        Console.WriteLine(lang);

        var response = await chatClient.CompleteAsync<UmbrellaNeedResult>(
            "Do I need an umbrella? When replying include weather field populating it with raw value what you get from the tool",
            options: new ChatOptions
            {
                Tools = [AIFunctionFactory.Create(GetWeather)]
            });
        if (response.Result is not null)
            Console.WriteLine(response.Result.needed ? "Umbrella is needed" : "Umbrella is not needed");
        else
        {
            Console.WriteLine("Couldnt figure out");
            Console.WriteLine(response);
        }    
    }
}
