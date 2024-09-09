using System.Threading.Tasks;

namespace ChatBots;

internal static class Program
{
    static async Task Main(string[] args)
    {
        //OpenAI.Run();
        //ML.MLTest();

        //OpenAITools.ChatWithTools();
        
        //await AutoGen.HelloWorldPhi3Async();
        await AutoGenChats.HelloAgents();
        // https://microsoft.github.io/autogen-for-net/articles/Built-in-messages.html

        //await SemanticKernelChats.HelloWorldAsync();
        //await SemanticKernelChats.PromptScenarioAsync();
        //await SemanticKernelChats.LightsPluginAsync();    // User: Please toggle the light
        //await SemanticKernelChats.RagScenarioAsync();     // not working because of Semantic kernel with ollama text embedding search does not return any value
        // https://github.com/microsoft/semantic-kernel/issues/6483
    }
}
