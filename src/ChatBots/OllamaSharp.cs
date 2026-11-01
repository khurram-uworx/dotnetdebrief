using OllamaSharp;
using OllamaSharp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ChatBots;

static class OllamaSharp
{
    public static async Task RunAsync(string urlOllama, string model)
    {
        //https://devblogs.microsoft.com/dotnet/alttext-generator-csharp-local-models/

        var ollama = new OllamaApiClient(new Uri(urlOllama));
        ollama.SelectedModel = model;
        var chat = new Chat(ollama);

        byte[] imageBytes = File.ReadAllBytes(@"Resources\SampleImage.jpg");
        var imageBytesEnumerable = new List<IEnumerable<byte>> { imageBytes };

        var message = "Describe the attached image. The description should be detailed and suitable for visually impaired users. Do not include any information about the image file name or format.";
        await foreach (var answerToken in chat.SendAsync(message: message, imagesAsBytes: imageBytesEnumerable))
            Console.Write(answerToken);

        Console.WriteLine($">> Ollama done"); ;
    }

    public static async Task RunSystemOneAsync(string urlOllama, string model)
    {
        var ollama = new OllamaApiClient(new Uri(urlOllama));
        var response = await ollama.SystemOneAsync(new SystemOneRequest
        {
            Model = model,
            State = "Our checkout has returned 500 errors since 9am.",
            Questions = new()
            {
                ["label"] = new SystemOneChoiceQuestion
                {
                    Instructions = "Which label fits this ticket?",
                    Criteria = new() { ["billing"] = "Payments and refunds", ["bug"] = "Software errors", ["account"] = "Login and account access" }
                },
                ["refund"] = new SystemOneNoulQuestion { Instructions = "Is the customer requesting a refund?" },
                ["urgency"] = new SystemOneScoreQuestion
                {
                    Instructions = "How urgently does this ticket need a response?",
                    Criteria = ["Routine", "Soon", "Immediate"]
                }
            }
        });

        var label = (SystemOneChoiceAnswer)response.Answers["label"];     // label.Choice == "bug"
        var refund = (SystemOneNoulAnswer)response.Answers["refund"];     // refund.Noul == probability of "yes"
        var urgency = (SystemOneScoreAnswer)response.Answers["urgency"];  // urgency.Score from 0 to 2

        Console.WriteLine($"Label: {label.Choice}");
        Console.WriteLine($"Refund: {refund.Noul}");
        Console.WriteLine($"Urgency: {urgency.Score}");
    }
}
