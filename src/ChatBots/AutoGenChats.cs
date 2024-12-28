using AutoGen;
using AutoGen.Core;
using System.Net.Http;
using System;
using AutoGen.Ollama;
using AutoGen.Ollama.Extension;
using System.Threading.Tasks;

namespace ChatBots;

internal static class AutoGenChats
{
    public static async Task HelloWorldGptAsync()
    {
        //https://microsoft.github.io/autogen-for-net/index.html

        var openAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new Exception("Please set OPENAI_API_KEY environment variable.");
        var gpt35Config = new OpenAIConfig(openAIKey, "gpt-3.5-turbo");

        var assistantAgent = new AssistantAgent(
            name: "assistant",
            systemMessage: "You are an assistant that help user to do some tasks.",
            llmConfig: new ConversableAgentConfig
            {
                Temperature = 0,
                ConfigList = [gpt35Config],
            })
            .RegisterPrintMessage(); // register a hook to print message nicely to console

        // set human input mode to ALWAYS so that user always provide input
        var userProxyAgent = new UserProxyAgent(
            name: "user",
            humanInputMode: HumanInputMode.ALWAYS)
            .RegisterPrintMessage();

        // start the conversation
        await userProxyAgent.InitiateChatAsync(
            receiver: assistantAgent,
            message: "Hey assistant, please do me a favor.",
            maxRound: 10);
    }

    public static async Task HelloOllamaWorldAsync(string textModel)
    {
        //https://microsoft.github.io/autogen-for-net/articles/AutoGen.Ollama/Chat-with-llama.html
        //
        using var httpClient = new HttpClient()
        {
            BaseAddress = new Uri("http://localhost:11434"),
        };

        var ollamaAgent = new OllamaAgent(
            httpClient: httpClient,
            modelName: textModel,
            name: "ollama",
            systemMessage: "You are a helpful AI assistant")
            .RegisterMessageConnector()
            .RegisterPrintMessage();

        var reply = await ollamaAgent.SendAsync("Can you write a piece of C# code to calculate 100th of fibonacci?");
        Console.WriteLine($"{reply.From} {reply.GetContent()}");
    }

    public static async Task HelloAgents(string textModel)
    {
        //https://microsoft.github.io/autogen-for-net/articles/Two-agent-chat.html

        using var httpClient = new HttpClient()
        {
            BaseAddress = new Uri("http://localhost:11434"),
            Timeout = TimeSpan.FromMinutes(5)
        };

        // create teacher agent
        // teacher agent will create math questions
        var teacher = new OllamaAgent(
            httpClient: httpClient,
            modelName: textModel,
            name: "teacher",
            systemMessage: @"You are a teacher that create pre-school math question for student and check answer.
If the answer is correct, you stop the conversation by saying [COMPLETE].
If the answer is wrong, you ask student to fix it.")
            .RegisterMessageConnector()
            .RegisterMiddleware(async (msgs, option, agent, _) =>
            {
                try
                {
                    var reply = await agent.GenerateReplyAsync(msgs, option);
                    //if (reply.GetContent()?.ToLower().Contains("complete") is true)
                    //{
                    //    return new TextMessage(Role.Assistant, GroupChatExtension.TERMINATE, from: reply.From);
                    //}

                    return reply;
                    
                    //var list = new List<TextMessage>();
                    //foreach(var msg in msgs)
                    //{
                    //    list.Add(new TextMessage(msg.GetRole() ?? Role.User, msg.GetContent(), msg.From));
                    //    //Console.WriteLine($"{msg.GetType()} and {msg is IMessage<AutoGen.Core.Message>}");
                    //}

                    //var reply = await agent.GenerateReplyAsync(list, option);
                    //if (reply.GetContent()?.ToLower().Contains("complete") is true)
                    //    return new TextMessage(Role.Assistant, GroupChatExtension.TERMINATE, from: reply.From);

                    //return reply;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception in teacher Middleware: {ex.Message}");
                    return await agent.GenerateReplyAsync(msgs);
                }
            })
            .RegisterPrintMessage();

        // create student agent
        // student agent will answer the math questions
        var student = new OllamaAgent(
            httpClient: httpClient,
            modelName: textModel, //modelName: "phi3:latest",
            name: "student",
            systemMessage: "You are a student that answer question from teacher")
            .RegisterMessageConnector()
            .RegisterPrintMessage();

        //// Create initial message
        //var initialMessage = new TextMessage(
        //    role: Role.User,
        //    content: "Hey teacher, please create a math question for me."
        //);

        // start the conversation
        var conversation = await student.InitiateChatAsync(
            receiver: teacher,
            message: "Hey teacher, please create math question for me.",
            maxRound: 10);
    }
}
