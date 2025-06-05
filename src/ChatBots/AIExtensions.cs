using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatBots;

static class AIExtensions
{
    enum Sentiment { Positive, Negative, Neutral }
    record SentimentRecord(string ResponseText, Sentiment ReviewSentiment);

    public static async Task StructuredOutputAsync(string urlOllama, string textModel)
    {
        //https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/structured-output
        IChatClient chatClient = new OllamaChatClient(new Uri(urlOllama), textModel)
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();

        string[] inputs = [
            "Best purchase ever!",
            "Returned it immediately.",
            "Hello",
            "It works as advertised.",
            "The packaging was damaged but otherwise okay."
        ];

        foreach (var i in inputs)
        {
            var response = await chatClient.GetResponseAsync<SentimentRecord>($"What's the sentiment of this review? {i}");
            if (response.Result is not null)
                Console.WriteLine($"Review: {i} | Sentiment: {response.Result.ReviewSentiment}, Response: {response.Result.ResponseText}");
            else
                Console.WriteLine($"Review: {i} | Unable to determine");
        }
    }

    public static async Task FunctionCallingAsync(string urlOllama, string textModel)
    {
        //https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/use-function-calling
        IChatClient client = new OllamaChatClient(new Uri(urlOllama), textModel)
            .AsBuilder()
            .UseFunctionInvocation()
            .Build();

        var chatOptions = new ChatOptions
        {
            Tools = [AIFunctionFactory.Create((string location, string unit) =>
            {
                // Here you would call a weather API
                // to get the weather for the location.
                return "Periods of rain or drizzle, 15 C";
            },
            "get_current_weather",
            "Get the current weather in a given location")]
        };

        // System prompt to provide context.
        List<ChatMessage> chatHistory = [
            new(ChatRole.System, """
                You are a hiking enthusiast who helps people discover fun hikes in their area. You are upbeat and friendly.
                Dont ask any further questions, try to answer with available information or using function calling
            """)];

        // Weather conversation relevant to the registered function.
        chatHistory.Add(new ChatMessage(ChatRole.User,
            "I live in Islamabad and I'm looking for a moderate intensity hike. What's the current weather like?"));
        Console.WriteLine($"{chatHistory.Last().Role} >>> {chatHistory.Last()}");

        ChatResponse response = await client.GetResponseAsync(chatHistory, chatOptions);
        Console.WriteLine($"Assistant >>> {response.Text}");
    }
}
