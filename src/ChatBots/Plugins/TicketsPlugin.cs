using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ChatBots.Plugins;

class TicketsPlugin
{
    [KernelFunction("check_known_issue")]
    [Description("Checks if the issue is a known problem")]
    public string CheckKnownIssue(
            [Required]
            [Description("The primary category of the issue")]
            string primaryCategory,
            [Required]
            [Description("The identification of the customer")]
            string customerIdentification,
            [Description("Details of the issue extracted from the ticket")]
            string details)
    {
        if (string.IsNullOrEmpty(primaryCategory)) return "Without primary categroy i cannt tell if its a known issue or not";
        if (string.IsNullOrEmpty(customerIdentification)) return "Without primary categroy i cannt tell if its a known issue or not";

        // why we need vectorization here
        if (primaryCategory is "Degradation") return "This is known issue at the moment";
        if (details is not null &&
            (details.Contains("degradation", System.StringComparison.InvariantCultureIgnoreCase)        // poor
            || details.Contains("slow", System.StringComparison.InvariantCultureIgnoreCase)             // semantic
            || details.Contains("outage", System.StringComparison.InvariantCultureIgnoreCase)))         // similarity
            return "This is known issue at the moment";

        return $"{primaryCategory} for {customerIdentification} having {details} is not a known issue";
    }
}
