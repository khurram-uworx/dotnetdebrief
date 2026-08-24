using System;
using System.Collections.Generic;
using System.Linq;

namespace ChatBots.ResumeScorer;

static class ScoreCalculator
{
    static string recommendation(double score) =>
        score >= 70 ? "Interview" : score >= 50 ? "Consider" : "Reject";

    static double ratio(int matched, int missing)
    {
        var denominator = matched + missing;
        return denominator == 0 ? 1.0 : (double)matched / denominator;
    }

    static List<string> normalize(List<string>? list) =>
        list?.Where(s => !string.IsNullOrWhiteSpace(s)).ToList() ?? new List<string>();

    static List<string> dedupCaseInsensitive(List<string> list)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();
        foreach (var item in list)
            if (seen.Add(item))
                result.Add(item);

        return result;
    }

    public static (double Score, string Recommendation) Calculate(ScoreBreakdown? breakdown)
    {
        breakdown ??= new ScoreBreakdown(new List<string>(), new List<string>(),
            new List<MatchedSection>(), new List<string>(), 0);

        var matched = normalize(breakdown.MatchedSkills);
        var missing = normalize(breakdown.MissingSkills);

        var dedupedMatched = dedupCaseInsensitive(matched);
        var matchedLower = dedupedMatched
            .Select(s => s.ToLowerInvariant())
            .ToHashSet();

        var dedupedMissing = missing
            .Where(s => !matchedLower.Contains(s.ToLowerInvariant()))
            .ToList();

        var skillsRatio = ratio(dedupedMatched.Count, dedupedMissing.Count);

        var matchedSections = breakdown.MatchedSections?.Count ?? 0;
        var missingRequirements = normalize(breakdown.MissingRequirements).Count;
        var reqRatio = ratio(matchedSections, missingRequirements);

        var holistic = breakdown.HolisticAtsScore;
        if (holistic < 0) holistic = 0;
        if (holistic > 100) holistic = 100;

        var raw = skillsRatio * 40 + reqRatio * 40 + holistic * 0.20;
        var score = Math.Clamp(Math.Round(raw, MidpointRounding.AwayFromZero), 0, 100);

        return (score, recommendation(score));
    }
}
