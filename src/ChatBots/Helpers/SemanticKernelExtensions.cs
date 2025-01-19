using Microsoft.KernelMemory;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace Helpers
{
    internal static class SemanticKernelExtensions
    {
        private static async Task<string> GetLongTermMemory(IKernelMemory memory, string query, bool asChunks = true)
        {
            if (asChunks)
            {
                // Fetch raw chunks, using KM indexes. More tokens to process with the chat history, but only one LLM request.
                SearchResult memories = await memory.SearchAsync(query, limit: 10);
                return memories.Results.SelectMany(m => m.Partitions).Aggregate("", (sum, chunk) => sum + chunk.Text + "\n").Trim();
            }

            // Use KM to generate an answer. Fewer tokens, but one extra LLM request.
            MemoryAnswer answer = await memory.AskAsync(query);
            return answer.Result.Trim();
        }

        public static async Task ChatLoop(this Kernel kernel, IKernelMemory memory,
            string systemPrompt, bool isStreaming,
            PromptExecutionSettings settings = null)
        {
            var chatService = kernel.GetRequiredService<IChatCompletionService>();
            var chatHistory = new ChatHistory(systemPrompt);

            // Start the chat
            var assistantMessage = "Hello, how can I help?";
            Console.WriteLine($"Assistant> {assistantMessage}\n");
            chatHistory.AddAssistantMessage(assistantMessage);

            // Infinite chat loop
            var reply = new StringBuilder();

            while (true)
            {
                // Get user message (retry if the user enters an empty string)
                Console.Write("You> ");
                var userMessage = Console.ReadLine()?.Trim();
                if (string.IsNullOrWhiteSpace(userMessage)) { break; }
                else { chatHistory.AddUserMessage(userMessage); }

                // Recall relevant information from memory
                var longTermMemory = await GetLongTermMemory(memory, userMessage);
                // Console.WriteLine("-------------------------- recall from memory\n{longTermMemory}\n--------------------------");

                // Inject the memory recall in the initial system message
                chatHistory[0].Content = $"{systemPrompt}\n\nLong term memory:\n{longTermMemory}";

                reply.Clear();

                // Get the response from the AI
                if (isStreaming)
                {
                    Console.Write("\nAssistant> ");

                    await foreach (StreamingChatMessageContent stream in chatService.GetStreamingChatMessageContentsAsync(
                        chatHistory, executionSettings: settings, kernel: kernel))
                    {
                        Console.Write(stream.Content);
                        reply.Append(stream.Content);
                    }
                }
                else
                {
                    var aiReply = await chatService.GetChatMessageContentAsync(
                        chatHistory,
                        executionSettings: settings,
                        kernel: kernel);

                    Console.WriteLine("Assistant> " + aiReply);

                    // Add the message from the agent to the chat history
                    chatHistory.AddMessage(aiReply.Role, aiReply.Content ?? string.Empty);
                }

                chatHistory.AddAssistantMessage(reply.ToString());
                Console.WriteLine("\n");
            }
        }
    }
}
