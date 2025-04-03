using Microsoft.Extensions.AI;
using System.Runtime.CompilerServices;

namespace Notepad.GenAI
{
    class OllamaGateway
    {
        string url = null;

        public OllamaGateway(string url) => this.url = url;

        public async IAsyncEnumerable<string> MakeItProfessional(string textModel, string text,
            [EnumeratorCancellation] CancellationToken token)
        {
            IChatClient chatClient = new OllamaChatClient(new Uri(this.url), textModel)
                .AsBuilder()
                .Build();

            List<ChatMessage> chatMessages = [];
            chatMessages.Add(new ChatMessage(ChatRole.System,
                //"""
                //You are a text refinement assistant. Your only task is to revise the given text
                //to make it more formal while preserving its original meaning. Do not generate
                //any additional content, explanations, or code—only return the revised text
                //"""));
                """
                Revise the given text to be more formal. Do not change the meaning. Do not add
                or remove any details. Do not generate code or explanations. Only return the revised text
                """));
            chatMessages.Add(new ChatMessage(ChatRole.User, text));

            await foreach (var update in chatClient.GetStreamingResponseAsync(chatMessages, cancellationToken: token))
                if (update.Text is not null) yield return update.Text;
        }

        public async IAsyncEnumerable<string> PassToLanguageModel(string textModel, string text,
            [EnumeratorCancellation] CancellationToken token)
        {
            IChatClient chatClient = new OllamaChatClient(new Uri(this.url), textModel)
                .AsBuilder()
                .Build();

            await foreach (var update in chatClient.GetStreamingResponseAsync(text, cancellationToken: token))
                if (update.Text is not null) yield return update.Text;
        }
    }
}
