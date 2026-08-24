using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ChatBots.ResumeScorer;

static class ResultsWriter
{
    static string escape(string value) =>
        value.Replace("\\", "\\\\").Replace("|", "\\|").Replace("\n", " ").Replace("\r", "").Trim();

    public static void Write(
        string outputPath,
        string jobDescription,
        IReadOnlyList<CandidateResult> results,
        IReadOnlyList<(string FileName, string Error)> failures)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Resume Scoring Results");
        builder.AppendLine();
        builder.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
        builder.AppendLine($"Scored: {results.Count} | Failed: {failures.Count}");
        builder.AppendLine();

        builder.AppendLine("| Name | Graduation Year | University | CGPA | ATS Score | Recommendation |");
        builder.AppendLine("|------|-----------------|------------|------|-----------|----------------|");

        foreach (var r in results)
        {
            var name = escape(r.Fields.Name ?? "Unknown");
            var year = r.Fields.GraduationYear?.ToString() ?? "";
            var university = escape(r.Fields.University ?? "");
            var cgpa = r.Fields.Cgpa?.ToString("F2") ?? "";
            builder.AppendLine(
                $"| {name} | {year} | {university} | {cgpa} | {r.Score:0} | {r.Recommendation} |");
        }

        if (failures.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Failures");
            builder.AppendLine();
            builder.AppendLine("The following files could not be scored and were skipped:");
            builder.AppendLine();
            foreach (var (fileName, error) in failures)
            {
                builder.AppendLine($"- **{escape(fileName)}**: {escape(error)}");
            }
        }

        File.WriteAllText(outputPath, builder.ToString());
    }
}
