using Microsoft.Extensions.AI;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ChatBots.OpenCode;

public static class OpenCodeExamples
{
    public static async Task StreamingAsync()
    {
        using var client = new OpenCodeClient();
        var chatClient = new OpenCodeChatClient(client);

        Console.Write("OpenCode: ");
        await foreach (var update in chatClient.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "Why is the sky blue?")]))
        {
            foreach (var content in update.Contents.OfType<TextContent>())
                Console.Write(content.Text);
        }
        Console.WriteLine();
    }
}
