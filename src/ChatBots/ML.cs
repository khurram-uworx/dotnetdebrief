using Microsoft.ML.OnnxRuntimeGenAI;
using System;

namespace ChatBots;

internal static class ML
{
    public static void MLTest()
    {
        //string modelPath = @"C:\Users\khurram\.aitk\models\microsoft\Phi-3-mini-128k-instruct-onnx\directml\directml-int4-awq-block-128";
        //string modelPath = @"C:\Users\khurram\.aitk\models\microsoft\Phi-3-mini-4k-instruct-onnx\cpu_and_mobile\cpu-int4-rtn-block-32-acc-level-4";
        string modelPath = @"Models\directml\directml-int4-awq-block-128";
        Console.Write("Loading model from " + modelPath + "...");
        using Model model = new(modelPath);
        Console.Write("Done\n");

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
            generatorParams.SetInputSequences(sequences);

            Console.Out.Write("\nAI: ");
            using Generator generator = new(model, generatorParams);
            while (!generator.IsDone())
            {
                generator.ComputeLogits();
                generator.GenerateNextToken();
                Console.Out.Write(tokenizerStream.Decode(generator.GetSequence(0)[^1]));
                Console.Out.Flush();
            }
            Console.WriteLine();
        }
    }
}
