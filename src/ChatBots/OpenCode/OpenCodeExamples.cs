using Chess;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ChatBots.OpenCode;

public static class OpenCodeExamples
{
    static async Task<string?> getGptMoveAsync(IChatClient chatClient, ChessBoard board, string previousInvalidMove = null)
    {
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

    public static async Task StreamingAsync()
    {
        using var client = new OpenCodeClient();
        var chatClient = new OpenCodeChatClient(client);

        Console.Write("OpenCode: ");
        await foreach (var update in chatClient.GetStreamingResponseAsync(
            [new ChatMessage(ChatRole.User, "Why is the sky blue?")]))
        {
            foreach (var content in update.Contents.OfType<TextContent>())
                Console.Write(content.Text);
        }
        Console.WriteLine();
    }

    public static async Task PlayChessAsync(IChatClient chatClient)
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
                string? aiMove = await getGptMoveAsync(chatClient, board);

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
