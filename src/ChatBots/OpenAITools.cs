using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Text.Json;

namespace ChatBots;

internal static class OpenAITools
{
    //https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/openai/Azure.AI.OpenAI/tests/Samples/01_Chat.cs

    public static void ChatWithTools(string urlOllama, string textModel)
    {
        static string GetCurrentWeather(string location, string unit = "celsius")
        {
            //Debugger.Break();
            return $"10 {unit}";
        }

        ChatTool getCurrentWeatherTool = ChatTool.CreateFunctionTool(
            functionName: nameof(GetCurrentWeather),
            functionDescription: "Get the current weather in a given location",
            functionParameters: BinaryData.FromString("""
            {
                "type": "object",
                "properties": {
                    "location": {
                        "type": "string",
                        "description": "The city"
                    },
                    "unit": {
                        "type": "string",
                        "enum": [ "celsius", "fahrenheit" ],
                        "description": "The temperature unit to use. Infer this from the specified location."
                    }
                },
                "required": [ "location" ]
            }
            """)
        );

        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri($"{urlOllama}/v1") // Ollama
        };

        var openAIClient = new OpenAIClient(new ApiKeyCredential("x"), clientOptions);
        var chatClient = openAIClient.GetChatClient(textModel);

        ChatCompletionOptions options = new()
        {
            Tools = { getCurrentWeatherTool },
        };

        Console.Write("User: [What's the weather like in Faisalabad?] ");
        string input = Console.ReadLine();
        if (string.IsNullOrEmpty(input)) input = "What's the weather like in Faisalabad?";

        List<ChatMessage> conversationMessages =
            [
                //https://github.com/microsoft/Phi-3CookBook/issues/13
                new SystemChatMessage("You are a virtual assistant capable of handling various tasks through a series of tools..."), //File.ReadAllText("Prompts.OpenAITools.System.txt")),
                new UserChatMessage(input),
            ];
        ChatCompletion completion = chatClient.CompleteChat(conversationMessages, options);

        // Purely for convenience and clarity, this standalone local method handles tool call responses.
        string GetToolCallContent(ChatToolCall toolCall)
        {
            if (toolCall.FunctionName == getCurrentWeatherTool.FunctionName)
            {
                // Validate arguments before using them; it's not always guaranteed to be valid JSON!
                try
                {
                    using JsonDocument argumentsDocument = JsonDocument.Parse(toolCall.FunctionArguments);
                    if (!argumentsDocument.RootElement.TryGetProperty("location", out JsonElement locationElement))
                    {
                        // Handle missing required "location" argument
                    }
                    else
                    {
                        string location = locationElement.GetString();
                        if (argumentsDocument.RootElement.TryGetProperty("unit", out JsonElement unitElement))
                        {
                            return GetCurrentWeather(location, unitElement.GetString());
                        }
                        else
                        {
                            return GetCurrentWeather(location);
                        }
                    }
                }
                catch (JsonException)
                {
                    // Handle the JsonException (bad arguments) here
                }
            }
            // Handle unexpected tool calls
            throw new NotImplementedException();
        }

        if (completion.FinishReason == ChatFinishReason.ToolCalls)
        {
            // Add a new assistant message to the conversation history that includes the tool calls
            conversationMessages.Add(new AssistantChatMessage(completion));

            foreach (ChatToolCall toolCall in completion.ToolCalls)
            {
                conversationMessages.Add(new ToolChatMessage(toolCall.Id, GetToolCallContent(toolCall)));
            }

            // Now make a new request with all the messages thus far, including the original
            completion = chatClient.CompleteChat(conversationMessages);
        }

        Console.WriteLine($"{completion.Role}: {completion.Content[0].Text}");
    }
}
