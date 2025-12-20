using Microsoft.ML.OnnxRuntimeGenAI;

namespace Scenarios;

static class DirectMLScenarios
{
    public static void Run(bool gpu, string textModel, bool npu = false)
    {
        string modelPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
            "..", "..", "..", textModel));
        Console.Write("Loading model from " + modelPath + "...");

        var config = new Config(modelPath);
        if (npu) config.AppendProvider("OpenVINO");
        else if (gpu) config.AppendProvider("DML");

        using Model model = new(config);//new(modelPath);
        Console.WriteLine("Done");

        using Tokenizer tokenizer = new(model);
        using TokenizerStream tokenizerStream = tokenizer.CreateStream();

        while (true)
        {
            Console.Write("User: ");
            string? input = Console.ReadLine();

            if (input == "quit") break;

            string prompt = "<|user|>\n" + input + "<|end|>\n<|assistant|>";

            var sequences = tokenizer.Encode(prompt);

            using GeneratorParams generatorParams = new GeneratorParams(model);
            generatorParams.SetSearchOption("max_length", 200);
            //generatorParams.SetInputSequences(sequences);

            Console.Out.Write("\nAI: ");
            
            using Generator generator = new(model, generatorParams);
            generator.AppendTokenSequences(sequences);

            while (!generator.IsDone())
            {
                //generator.ComputeLogits(); // looks like we dont need this anymore
                generator.GenerateNextToken();
                Console.Out.Write(tokenizerStream.Decode(generator.GetSequence(0)[^1]));
                Console.Out.Flush();
            }
            Console.WriteLine();
        }
    }
}
