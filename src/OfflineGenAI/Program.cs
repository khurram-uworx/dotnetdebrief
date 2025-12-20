using LLama;
using LLama.Common;
using Scenarios;

Console.Title = "Offline GenAI";
Console.WriteLine("Offline GenAI — Running things completely offline");

Console.WriteLine("\nChoose demo:");
Console.WriteLine("1 - Chat");
//Console.WriteLine("2 - Tool-Using Agent");
//Console.WriteLine("3 - Code Review");
//Console.WriteLine("4 - Self-Critique Loop");
Console.WriteLine("11 - DirectML (CPU)");
Console.WriteLine("12 - DirectML (GPU)");
Console.WriteLine("13 - DirectML (GPU)");
Console.WriteLine("21 - Foundry Local (GPU/NPU/CPU)");
Console.WriteLine("0 - Exit");
Console.Write("> ");

var input = Console.ReadLine();

if (input == "0")
    return;
else if (input == "11")
    //
    /*
        * https://github.com/microsoft/onnxruntime-genai
        * 
        * hf download microsoft/Phi-3-mini-4k-instruct-onnx --include cpu_and_mobile/cpu-int4-rtn-block-32-acc-level-4/* --local-dir .
        * hf download microsoft/Phi-3-mini-4k-instruct-onnx --include "directml/*" --local-dir .
        *  directml-int4-awq-block-128
        */

    DirectMLScenarios.Run(gpu: false, textModel: "cpu_and_mobile\\cpu-int4-rtn-block-32-acc-level-4"); //directml-int4-awq-block-128
else if (input == "12")
    DirectMLScenarios.Run(gpu: true, textModel: "directml\\directml-int4-awq-block-128");
else if (input == "13")
    DirectMLScenarios.Run(gpu: true, textModel: "cpu_and_mobile\\cpu-int4-rtn-block-32-acc-level-4", npu: true);
else if (input == "21")
    await new FoundryScenario().RunAsync();
else
{
    //var modelPath = "models/mistral-7b-instruct.Q4_K_M.gguf";
    var modelPath = @"C:\Users\khurram\AppData\Local\llama.cpp\LiquidAI_LFM2-1.2B-Tool-GGUF_LFM2-1.2B-Tool-Q4_K_M.gguf";

    var parameters = new ModelParams(modelPath)
    {
        Embeddings = false,
        ContextSize = 2048,
        GpuLayerCount = 32
        //Threads = Environment.ProcessorCount,
        //UseMemoryLock = false,
        //UseMemoryMap = true
    };

    using var model = LLamaWeights.LoadFromFile(parameters);
    //var executor = new StatelessExecutor(model, parameters, logger: Utils.GetAppLogger());
    using var context = model.CreateContext(parameters);

    // https://scisharp.github.io/LLamaSharp/0.25.0/Tutorials/Executors/
    var executor = new InteractiveExecutor(context, Utils.GetAppLogger());
    // var executor = new InstructExecutor(context, logger: Utils.GetAppLogger());
    // BatchedExecutor

    // https://github.com/Liquid4All/leap-llamacpp-csharp-example/blob/main/LiquidLlamaSharp/Program.cs
    // Add chat histories as prompt to tell AI how to act.
    var chatHistory = new ChatHistory();

    ChatSession session = new(executor, chatHistory);

    InferenceParams inferenceParams = new InferenceParams()
    {
        MaxTokens = 2048,
        AntiPrompts = new List<string> { "User:" } // Stop generation once antiprompts appear.
    };

    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.Write("The chat session has started. Type `.exit` to terminate the session.\nUser: ");
    Console.ForegroundColor = ConsoleColor.Green;
    string userInput = Console.ReadLine() ?? "";

    while (userInput != ".exit")
    {
        await foreach ( // Generate the response streamingly.
            var text
            in session.ChatAsync(
                new ChatHistory.Message(AuthorRole.User, userInput),
                inferenceParams))
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(text);
        }
        Console.ForegroundColor = ConsoleColor.Green;
        userInput = Console.ReadLine() ?? "";
    }

    //switch (input)
    //{
    //    case "1":
    //        await ChatScenario.RunAsync(executor);
    //        break;
    //    case "2":
    //        await ToolAgentScenario.RunAsync(executor);
    //        break;
    //    case "3":
    //        await CodeReviewScenario.RunAsync(executor);
    //        break;
    //    case "4":
    //        await SelfCritiqueScenario.RunAsync(executor);
    //        break;
    //}
}
