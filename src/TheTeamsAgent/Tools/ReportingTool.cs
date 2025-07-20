using System.ComponentModel;

using ModelContextProtocol.Server;

namespace Tools;

[McpServerToolType]
public class ReportingTool
{
    [McpServerTool, Description("Generates a report for the specified name and team. This generate the special application specific url that can be sent to user as is")]
    public static string GenerateReport(string name, string team)
        => $"report://{team}/{name}.pdf";

    [McpServerTool, Description("Gives the historical data for the specified name and team. Its a good idea to use this data and include the trend analysis when generating any report")]
    public static string Summarize(string name, string team)
    {
         return team switch
        {
            "Alpha" => "2 Months earlier: 40sp, Last Month: 50sp, This Month: 35sp",
            "Bravo" => "2 Months earlier: 30sp, Last Month: 40sp, This Month: 50sp",
            _ => $"Summary for {name} in team {team} is not available."
        };
    }
}
