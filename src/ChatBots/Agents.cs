using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OllamaSharp;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace ChatBots;

class Agents
{
    //https://devblogs.microsoft.com/dotnet/introducing-microsoft-agent-framework-preview/

    [Description("Gets the author of the story.")]
    static string GetAuthor() => """
        The Brothers Grimm, Jacob (1785–1863) and Wilhelm (1786–1859)
        - They were German academics who together collected and published folklore.
        - The brothers are among the best-known storytellers of folktales
        """.Trim();

    [Description("Formats the story for display.")]
    static string FormatStory(string title, string author, string story) =>
        $"Title: {title}\nAuthor: {author}\n\n{story}";
    
    public static async Task WriterEditorAsync(string urlOllama, string model)
    {
        //IChatClient chatClient = new ChatClient("gpt-4o-mini",
        //    new ApiKeyCredential(Environment.GetEnvironmentVariable("GITHUB_TOKEN")!),
        //    new OpenAIClientOptions { Endpoint = new Uri("https://models.github.ai/inference") })
        //    .AsIChatClient();
        IChatClient chatClient = new OllamaApiClient(new Uri(urlOllama), model);
        var prompt = "Write a short story about Hansel and Gretel.";

        AIAgent writer = new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = "Writer",
                Instructions = "Write stories that are engaging and creative.",
                ChatOptions = new ChatOptions
                {
                    Tools = [
                        AIFunctionFactory.Create(GetAuthor),
                        AIFunctionFactory.Create(FormatStory)
                    ]
                }
            });
        
        AgentRunResponse response = await writer.RunAsync(prompt);
        Console.WriteLine("Single Agent Response:");
        Console.WriteLine(response.Text);

        AIAgent editor = new ChatClientAgent(
            chatClient,
            new ChatClientAgentOptions
            {
                Name = "Editor",
                Instructions = "Make the story more engaging, fix grammar, and enhance the plot."
            });

        Workflow workflow = AgentWorkflowBuilder.BuildSequential([writer, editor]);
        AIAgent workflowAgent = await workflow.AsAgentAsync();
        AgentRunResponse workflowResponse = await workflowAgent.RunAsync(prompt);

        Console.WriteLine("\n\n\nWorkflow Response:");
        Console.WriteLine(workflowResponse.Text);
    }
}
