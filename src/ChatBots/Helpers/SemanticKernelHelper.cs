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
        public static Kernel GetKernel(string openApiUrl, string openApiKey, string openApiModel, Action<IKernelBuilder, OpenAIClient> action = null)
        {
            var options = new OpenAIClientOptions
            {
                Endpoint = new Uri(openApiUrl),
                NetworkTimeout = TimeSpan.FromMinutes(5)
            };
            var openAIClient = new OpenAIClient(new ApiKeyCredential(openApiKey), options);

            // Create a chat completion service
            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion(
                modelId: openApiModel,
                openAIClient);

            // Add enterprise components
            builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Warning));

            if (null != action) action(builder, openAIClient);

            return builder.Build();
        }

        public static Kernel GetKernel(string urlOllama, string textModel, Action<IKernelBuilder, OpenAIClient> action = null) =>
            GetKernel(urlOllama, openApiKey: "x", textModel, action);
    }
}
