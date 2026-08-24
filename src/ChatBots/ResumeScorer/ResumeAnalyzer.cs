using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace ChatBots.ResumeScorer;

sealed class ResumeAnalyzer
{
    sealed record FlatEvaluation(
        CandidateFields? Fields,
        List<string>? MatchedSkills,
        List<string>? MissingSkills,
        List<MatchedSection>? MatchedSections,
        List<string>? MissingRequirements,
        double HolisticAtsScore);

    const string SystemPrompt = """
        You are an ATS (applicant tracking) scoring engine. You evaluate a single resume
        against a single job description and return a structured evaluation. Follow these rules:

        1. Extract candidate fields exactly: name, graduationYear (integer, or null if absent),
           university (string, or null), and cgpa (number 0-4 scale, or null if absent).
        2. Evaluate the job description's requirements with STRICT 1-to-1 matching: every
           requirement bullet is either fully matched (provide the exact requirement text plus
           the resume evidence proving it) or listed as missing. Never merge or split requirements.
        3. Extract matchedSkills and missingSkills as string arrays. Treat semantic synonyms as
           matches (e.g. React = React.js, Postgres = PostgreSQL, C# = CSharp).
        4. Never hallucinate qualifications. Use null/empty for absent data.
        5. Provide a holisticAtsScore from 0 to 100 reflecting overall fit.

        IMPORTANT: Do not output any reasoning, thinking, or commentary. Output ONLY a single
        JSON object and nothing else. Use exactly this structure (two top-level keys):
        {
          "fields": { "name": string, "graduationYear": int|null, "university": string|null, "cgpa": number|null },
          "breakdown": {
            "matchedSkills": [string],
            "missingSkills": [string],
            "matchedSections": [ { "requirement": string, "evidence": string } ],
            "missingRequirements": [string],
            "holisticAtsScore": number
          }
        }
        """;

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    static ResumeEvaluation parse(ChatResponse response)
    {
        var text = response.Text ?? throw new InvalidOperationException("Empty model response.");
        try
        {
            var json = extractJson(text);
            var evaluation = JsonSerializer.Deserialize<ResumeEvaluation>(json, JsonOptions);
            if (evaluation?.Breakdown is null)
            {
                var flat = JsonSerializer.Deserialize<FlatEvaluation>(json, JsonOptions);
                if (flat?.Fields is null)
                {
                    throw new InvalidOperationException("Model response missing fields/breakdown.");
                }
                evaluation = new ResumeEvaluation(flat.Fields, new ScoreBreakdown(
                    flat.MatchedSkills ?? new List<string>(),
                    flat.MissingSkills ?? new List<string>(),
                    flat.MatchedSections ?? new List<MatchedSection>(),
                    flat.MissingRequirements ?? new List<string>(),
                    flat.HolisticAtsScore));
            }
            return evaluation!;
        }
        catch (Exception)
        {
            throw;
        }
    }

    static string extractJson(string text)
    {
        var cleaned = stripThink(text).Trim();

        const string fence = "```";
        var fenceIdx = cleaned.IndexOf(fence, StringComparison.Ordinal);
        if (fenceIdx >= 0)
        {
            var afterFence = cleaned.Substring(fenceIdx + fence.Length);
            var secondFence = afterFence.IndexOf(fence, StringComparison.Ordinal);
            var inner = secondFence > 0 ? afterFence.Substring(0, secondFence) : afterFence;
            var newline = inner.IndexOf('\n');
            inner = newline >= 0 ? inner.Substring(newline + 1) : inner;
            var fenced = inner.Trim();
            if (fenced.StartsWith("{")) return fenced;
        }

        var firstBrace = cleaned.IndexOf('{');
        var lastBrace = cleaned.LastIndexOf('}');
        if (firstBrace >= 0 && lastBrace > firstBrace)
            return cleaned.Substring(firstBrace, lastBrace - firstBrace + 1);

        return cleaned;
    }

    static string stripThink(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;
        var result = text;
        int start;
        while ((start = result.IndexOf("<think>", StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            var end = result.IndexOf("</think>", StringComparison.OrdinalIgnoreCase);
            if (end < 0) break;
            result = result.Remove(start, end + "</think>".Length - start);
        }
        return result;
    }

    readonly IChatClient chatClient;

    public ResumeAnalyzer(IChatClient chatClient)
    {
        //var client = new OpenAIClient(
        //    new ApiKeyCredential(config.ApiKey),
        //    new OpenAIClientOptions { Endpoint = new Uri(config.BaseUrl) });
        //chatClient = client.GetChatClient(config.Model).AsIChatClient();
        this.chatClient = chatClient;
    }

    public async Task<ResumeEvaluation> EvaluateAsync(string jobDescription, string resumeText)
    {
        var userMessage = $"""
            # Job Description
            {jobDescription}

            # Resume
            {resumeText}
            """;

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPrompt),
            new(ChatRole.User, userMessage)
        };

        var options = new ChatOptions
        {
            Temperature = 0,
            MaxOutputTokens = 2048
        };

        try
        {
            return parse(await chatClient.GetResponseAsync(messages, options));
        }
        catch (Exception)
        {
            return parse(await chatClient.GetResponseAsync(messages, options));
        }
    }
}
