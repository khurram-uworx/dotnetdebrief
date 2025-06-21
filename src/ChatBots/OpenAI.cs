using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Linq;

namespace ChatBots;

static class OpenAI
{
    public static void Run(string openAiUrl, string openAiKey, string openAiModel)
    {
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri(openAiUrl)
        };

        var openAIClient = new OpenAIClient(new ApiKeyCredential(openAiKey), options);
        var chatClient = openAIClient.GetChatClient(openAiModel);
        var chatCompletion = chatClient.CompleteChat(
            [
                new SystemChatMessage("You are a helpful assistant. Be brief and succinct."),
                new UserChatMessage("What is the golden ratio?")
            ], new ChatCompletionOptions
            {
                Temperature = 0.7f 
            }
            ).Value;

        foreach (var content in chatCompletion.Content.Where(o => null != o && !string.IsNullOrEmpty(o.Text)))
            Console.WriteLine(content.Text);
    }
}
