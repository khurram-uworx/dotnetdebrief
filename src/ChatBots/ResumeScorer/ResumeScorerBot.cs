using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ChatBots.ResumeScorer;

static class ResumeScorerBot
{
    public static async Task<int> ScanFolderAsync(IChatClient chatClient, string folder)
    {
        //OpenAIConfig config;
        //try
        //{
        //    config = OpenAIConfigLoader.Load();
        //}
        //catch (InvalidOperationException ex)
        //{
        //    Console.Error.WriteLine(ex.Message);
        //    return 1;
        //}

        if (!Directory.Exists(folder))
        {
            Console.Error.WriteLine($"Folder not found: {folder}");
            return 2;
        }

        var jobPath = Path.Combine(folder, "JOB.md");
        if (!File.Exists(jobPath))
        {
            Console.Error.WriteLine($"JOB.md not found in folder: {folder}");
            return 2;
        }

        var jobDescription = await File.ReadAllTextAsync(jobPath);

        var resumeFiles = Directory
            .EnumerateFiles(folder)
            .Where(f =>
            {
                var info = new FileInfo(f);
                if (info.Attributes.HasFlag(FileAttributes.Hidden)) return false;
                if (Path.GetFileName(f).StartsWith('~')) return false;
                var ext = info.Extension.ToLowerInvariant();
                return ext is ".pdf" or ".docx" or ".txt";
            })
            .OrderBy(f => f)
            .ToList();

        if (resumeFiles.Count == 0)
        {
            Console.Error.WriteLine("No resumes found (.pdf, .docx, .txt) in the folder.");
            return 2;
        }

        var analyzer = new ResumeAnalyzer(chatClient);
        var results = new List<CandidateResult>();
        var failures = new List<(string FileName, string Error)>();

        foreach (var file in resumeFiles)
        {
            var fileName = Path.GetFileName(file);
            try
            {
                var text = ResumeReader.Read(file);
                var evaluation = await analyzer.EvaluateAsync(jobDescription, text);
                var (score, recommendation) = ScoreCalculator.Calculate(evaluation.Breakdown);
                results.Add(new CandidateResult(fileName, evaluation.Fields, score, recommendation));
                Console.WriteLine($"Scored {fileName}: {score:0} ({recommendation})");
            }
            catch (Exception ex)
            {
                failures.Add((fileName, ex.Message));
                Console.Error.WriteLine($"Failed {fileName}: {ex.Message}");
            }
        }

        results.Sort((a, b) => b.Score.CompareTo(a.Score));

        ResultsWriter.Write(folder, jobDescription, results, failures);

        Console.WriteLine($"{results.Count} scored, {failures.Count} failed");

        return 0;
    }
}
