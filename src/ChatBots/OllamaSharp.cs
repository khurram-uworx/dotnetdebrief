using OllamaSharp;
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
}
