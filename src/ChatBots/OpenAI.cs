using OpenAI;
using OpenAI.Chat;
using System;
using System.ClientModel;
using System.Linq;

namespace ChatBots;

internal static class OpenAI
{
    public static void Run(string urlOllama, string textModel)
    {
        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri($"{urlOllama}/v1") // Ollama
            // Endpoint = new Uri("http://127.0.0.1:5272/v1") // AI Toolkit
        };

        var openAIClient = new OpenAIClient(new ApiKeyCredential("x"), options);
        var chatClient = openAIClient.GetChatClient(textModel);
        //var chatClient = openAIClient.GetChatClient("Phi-3-mini-128k-cpu-int4-rtn-block-32-onnx");

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
