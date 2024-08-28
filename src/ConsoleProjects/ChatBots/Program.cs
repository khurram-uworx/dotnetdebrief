using System.Threading.Tasks;

namespace ChatBots;

internal static class Program
{
    static async Task Main(string[] args)
    {
        //OpenAI.Run();
        //ML.MLTest();

        OpenAITools.ChatWithTools();
    }
}
