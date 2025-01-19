using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using OpenAI;
using System;
using System.ClientModel;

namespace ChatBots.Helpers
{
    internal static class SemanticKernelHelper
    {
        public static Kernel GetKernel(string urlOllama, string textModel, Action<IKernelBuilder, OpenAIClient> action = null)
        {
            var options = new OpenAIClientOptions
            {
                Endpoint = new Uri($"{urlOllama}/v1"), // Ollama
                                                       // Endpoint = new Uri("http://127.0.0.1:5272/v1") // AI Toolkit
                NetworkTimeout = TimeSpan.FromMinutes(5)
            };
            var openAIClient = new OpenAIClient(new ApiKeyCredential("x"), options);

            // Create a chat completion service
            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion(
                modelId: textModel,
                openAIClient);

            // Add enterprise components
            builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Warning));

            if (null != action) action(builder, openAIClient);

            return builder.Build();
        }
    }
}
