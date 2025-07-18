using System.ComponentModel;

using ModelContextProtocol.Server;

namespace Tools;

[McpServerToolType]
public class ReportingTool
{
    [McpServerTool, Description("Generates a report for the specified name and team. This generate the special application specific url that can be sent to user as is")]
    public static string GenerateReport(string name, string team)
        => $"report://{team}/{name}.pdf";
}
