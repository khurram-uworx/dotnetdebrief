using Microsoft.ML.OnnxRuntimeGenAI;
using System;
using System.IO;

namespace ChatBots;

static class ML
{
    public static void MLTest(string textModel)
    {
        //string modelPath = @"C:\Users\khurram\.aitk\models\microsoft\Phi-3-mini-128k-instruct-onnx\directml\directml-int4-awq-block-128";
        //string modelPath = @"C:\Users\khurram\.aitk\models\microsoft\Phi-3-mini-4k-instruct-onnx\cpu_and_mobile\cpu-int4-rtn-block-32-acc-level-4";
        string modelPath = Path.Combine("Models", "directml", textModel);
        Console.Write("Loading model from " + modelPath + "...");
        using Model model = new(modelPath);
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
