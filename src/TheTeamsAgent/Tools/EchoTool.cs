using System.ComponentModel;
using System.IO;
using System.IO.Pipelines;
using System.Linq;

using ModelContextProtocol.Server;

namespace Tools;

[McpServerToolType]
public class EchoTool
{
    static Pipe clientToServer = new Pipe();
    static Pipe serverToClient = new Pipe();

    internal static Stream ServerInput => clientToServer.Reader.AsStream();
    internal static Stream ServerOutput => serverToClient.Writer.AsStream();

    internal static Stream ClientInput => serverToClient.Reader.AsStream();
    internal static Stream ClientOutput => clientToServer.Writer.AsStream();



    [McpServerTool, Description("Echoes the message back to the client.")]
    public static string Echo(string message)
        => $"Hello from C#: {message}";

    [McpServerTool, Description("Echoes in reverse the message sent by the client.")]
    public static string ReverseEcho(string message)
        => new string(message.Reverse().ToArray());
}
