using Chess;
using Microsoft.Extensions.AI;
using OllamaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatBots;

static class AIExtensions
{
    enum Sentiment { Positive, Negative, Neutral }
    record SentimentRecord(string ResponseText, Sentiment ReviewSentiment);

    static async Task<string?> getGptMoveAsync(string urlOllama, string textModel, ChessBoard board, string previousInvalidMove = null)
    {
        IChatClient chatClient = new OllamaApiClient(new Uri(urlOllama), textModel);
        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, """
            You are a chess engine. Respond only with the best move in UCI format
            - for pawn its a good idea to use from-to format like e2e4
            - for other pieces use piece destination format like nd2 to move n to d2"
            """)
        };

        if (previousInvalidMove != null)
            messages.Add(new(ChatRole.System, $"Your previously suggested move {previousInvalidMove} was invalid. Please suggest a valid move."));

        messages.Add(new(ChatRole.User, $"Its your turn now, you are Black, Current board FEN: {board.ToFen()}"));

        var response = await chatClient.GetResponseAsync(messages);
        return response is not null && !string.IsNullOrWhiteSpace(response.Text) ? response.Text.Trim() : null;
    }

    public static async Task StructuredOutputAsync(string urlOllama, string textModel)
    {
        //https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/structured-output
        IChatClient chatClient = new OllamaApiClient(new Uri(urlOllama), textModel);
            //.AsBuilder()
            //.UseFunctionInvocation()
            //.Build();

        string[] inputs = [
            "Best purchase ever!",
            "Returned it immediately.",
            "Hello",
            "It works as advertised.",
            "The packaging was damaged but otherwise okay."
        ];

        foreach (var i in inputs)
        {
            var response = await chatClient.GetResponseAsync<SentimentRecord>($"What's the sentiment of this review? {i}");
            if (response.Result is not null)
                Console.WriteLine($"Review: {i} | Sentiment: {response.Result.ReviewSentiment}, Response: {response.Result.ResponseText}");
            else
                Console.WriteLine($"Review: {i} | Unable to determine");
        }
    }

    public static async Task FunctionCallingAsync(string urlOllama, string textModel)
    {
        //https://learn.microsoft.com/en-us/dotnet/ai/quickstarts/use-function-calling
        IChatClient client = new OllamaApiClient(new Uri(urlOllama), textModel);
            //.AsBuilder()
            //.UseFunctionInvocation()
            //.Build();

        var chatOptions = new ChatOptions
        {
            Tools = [AIFunctionFactory.Create((string location, string unit) =>
            {
                // Here you would call a weather API
                // to get the weather for the location.
                return "Periods of rain or drizzle, 15 C";
            },
            "get_current_weather",
            "Get the current weather in a given location")]
        };

        // System prompt to provide context.
        List<ChatMessage> chatHistory = [
            new(ChatRole.System, """
                You are a hiking enthusiast who helps people discover fun hikes in their area. You are upbeat and friendly.
                Dont ask any further questions, try to answer with available information or using function calling
            """)];

        // Weather conversation relevant to the registered function.
        chatHistory.Add(new ChatMessage(ChatRole.User,
            "I live in Islamabad and I'm looking for a moderate intensity hike. What's the current weather like?"));
        Console.WriteLine($"{chatHistory.Last().Role} >>> {chatHistory.Last()}");

        ChatResponse response = await client.GetResponseAsync(chatHistory, chatOptions);
        Console.WriteLine($"Assistant >>> {response.Text}");
    }

    public static async Task PlayChessAsync(string urlOllama, string textModel)
    {
        var board = new ChessBoard();
        bool playerIsWhite = true;

        Console.WriteLine("Welcome to AI Chess!");

        while (!board.IsEndGame)
        {
            Console.WriteLine(board.ToAscii());

            if ((board.Turn == PieceColor.White && playerIsWhite) ||
                (board.Turn == PieceColor.Black && !playerIsWhite))
            {
                Console.Write("Your move (e.g., e2e4): ");
                string? moveInput = Console.ReadLine()?.Trim();

                if (string.IsNullOrWhiteSpace(moveInput) || !board.IsValidMove(moveInput))
                {
                    Console.WriteLine("Invalid move. Try again.");
                    continue;
                }

                board.Move(moveInput);
            }
            else
            {
            thinking:
                string previousInvalidMove = null;
                Console.WriteLine("thinking...");
                string? aiMove = await getGptMoveAsync(urlOllama, textModel, board);

                if (!string.IsNullOrWhiteSpace(aiMove))
                {
                    previousInvalidMove = aiMove;
                    Console.WriteLine($"AI plays: {aiMove}");

                    if (board.IsValidMove(aiMove))
                        board.Move(aiMove);
                    else
                        goto thinking;
                }
                else
                {
                    Console.WriteLine("GPT suggested an invalid move. Stopping game.");
                    break;
                }
            }
        }

        Console.WriteLine("Game Over!");
    }
}
