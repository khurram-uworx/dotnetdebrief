#pragma warning disable GHCP001

using GitHub.Copilot;
using GitHub.Copilot.Rpc;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ChatBots;

static class CopilotAgents
{
    static string setupWorkFolder(string ticketId)
    {
        // AppContext.BaseDirectory
        var directory = Path.Combine(Path.GetTempPath(), ticketId);
        Directory.CreateDirectory(directory);

        var filePath = Path.Combine(directory, "index.html");

        var htmlContent = """
            <html>
            <body>
                <div align=""left"">Hello World</div>
            </body>
            </html>
            """;

        File.WriteAllText(filePath, htmlContent);

        return directory;
    }

    public static async Task CopilotAgentWithToolsAsync()
    {
        //https://github.com/github/copilot-sdk/blob/main/docs/integrations/microsoft-agent-framework.md
        // Define a custom tool
        AIFunction requirementsTool = AIFunctionFactory.Create(
            (string ticket) => $"Center align the div in index.html",
            "GetRequirements",
            "Get the requirements for a given ticket"
        );

        await using var copilotClient = new CopilotClient();
        await copilotClient.StartAsync();

        //var done = new TaskCompletionSource(); // we can await done.Task;
        //copilotClient.On(evt =>
        //{
        //    //if (evt is AssistantMessageEvent msg)
        //    //    Console.WriteLine(msg.Data.Content);
        //    if (evt is SessionIdleEvent)
        //        done.SetResult();
        //});

        var workFolder = setupWorkFolder("TICKT1");
        var sessionConfig = new SessionConfig
        {
            //OnPermissionRequest = PermissionHandler.ApproveAll,
            OnPermissionRequest = async (req, inv) =>
            {
                Console.WriteLine($"\n[Permission Request: {req.Kind}]");

                //Console.Write("Approve? (y/n): ");
                //string? input = Console.ReadLine()?.Trim().ToUpperInvariant();
                //string kind = input is "Y" or "YES" ? "approved" : "denied-interactively-by-user";

                //return Task.FromResult(new PermissionRequestResult()
                //{
                //    Kind = PermissionRequestResultKind.Approved
                //});
                return PermissionDecision.ApproveOnce();
            },
            WorkingDirectory = workFolder,
            Tools = [requirementsTool]
        };

        //https://github.com/github/copilot-sdk/blob/main/docs/integrations/microsoft-agent-framework.md
        AIAgent agent = copilotClient.AsAIAgent(sessionConfig);
        var response = await agent.RunAsync("You are UI Developer, lets do the ticket TICKT-1");

        Console.WriteLine(response);
        Process.Start("notepad.exe", Path.Combine(workFolder, "index.html"));
    }
}
