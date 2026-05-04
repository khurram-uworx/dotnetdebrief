using AgentClientProtocol;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace ChatBots.Acp;

static class AcpLauncher
{
    public static async Task CopilotAcpAsync()
    {
        //https://github.blog/changelog/2026-01-28-acp-support-in-copilot-cli-is-now-in-public-preview/
        //using var copilotClient = new TcpClient("localhost", 8080);
        //NetworkStream stream = copilotClient.GetStream();
        //TextReader reader = new StreamReader(stream);
        //TextWriter writer = new StreamWriter(stream) { AutoFlush = true };

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                //FileName = "gemini",
                //Arguments = "--experimental-acp",
                FileName = "copilot",
                Arguments = "--acp",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
            },
        };

        try
        {
            if (!process.Start())
            {
                throw new Exception("Failed to start agent proccess");
            }

            var client = new ExampleClient();
            var connection = new ClientSideConnection(_ => client, process.StandardOutput, process.StandardInput);

            connection.Open();

            var initResult = await connection.InitializeAsync(new InitializeRequest
            {
                ProtocolVersion = 1,
                ClientCapabilities = new ClientCapabilities
                {
                    Fs = new FileSystemCapability
                    {
                        ReadTextFile = true,
                        WriteTextFile = true
                    }
                }
            });

            Console.WriteLine($"✅ Connected to agent (protocol v{initResult.ProtocolVersion})");

            var sessionResult = await connection.NewSessionAsync(new NewSessionRequest
            {
                Cwd = Directory.GetCurrentDirectory(),
                McpServers = []
            });

            Console.WriteLine($"📝 Created session: {sessionResult.SessionId}");
            Console.WriteLine("💬 User: Hello, agent!\n");
            Console.Write(" ");

            var promptResult = await connection.PromptAsync(new PromptRequest
            {
                SessionId = sessionResult.SessionId,
                Prompt = [new TextContentBlock { Text = "Hello, agent!" }]
            });

            Console.WriteLine($"\n\n✅ Agent completed with: {promptResult.StopReason}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[Client] Error: {ex}");
        }
        finally
        {
            process.Kill();
        }
    }
}
