# AGENTS.md — ResumeScorer

Instructions for AI agents working in this repository on the **ResumeScorer** project.

## Project

`src/ResumeScorer` is a .NET 10 console utility for recruiters: scan a folder of
candidate resumes (`.pdf`, `.docx`, `.txt`), read the job description from `JOB.md`
in that folder, and produce a ranked `Results.md` table
(Name | Graduation Year | University | CGPA | ATS Score | Recommendation, sorted best-first).

Before implementing anything, read these two files in the project folder:

1. **`src/ResumeScorer/README.md`** — what the tool does, usage, env vars.
2. **`src/ResumeScorer/ARCHITECTURE.md`** — authoritative design: file layout, packages,
   LLM contract, scoring formula, error handling, exit codes. Follow it; if you must
   deviate, call out the deviation explicitly.

The approved plan and scope decisions live in `src/ResumeScorer/PLAN.md`
(do not treat PLAN.md as an implementation instruction file).

## Environment

- SDKs installed: .NET 10.0.400 (stable) and .NET 11 preview. Target **`net10.0`**.
- Solution: add the project to `src/GenAI.sln`
  (`dotnet sln src/GenAI.sln add src/ResumeScorer/ResumeScorer.csproj`).
- NuGet packages to use: `Microsoft.Extensions.AI.OpenAI`, `PdfPig`,
  `DocumentFormat.OpenXml`.

## Configuration (env vars only — never hardcode)

- `OPENAI_BASE_URL` — e.g. `https://api.groq.com/openai/v1` (any OpenAI-compatible endpoint)
- `OPENAI_API_KEY`
- `OPENAI_MODEL` — e.g. `llama-3.3-70b-versatile`

Missing variables must fail fast with a clear message and exit code 1.

## Non-negotiable behavior

- Single LLM call per resume doing BOTH field extraction and scoring, temperature 0,
  structured JSON output per the contract in ARCHITECTURE.md.
- Final score recomputed deterministically in code:
  `skillsRatio×40 + requirementsRatio×40 + llmHolistic×0.20`, clamped 0–100;
  recommendations ≥70 Interview / ≥50 Consider / else Reject.
  Case-insensitive skill dedup before ratio math.
- One resume's failure must not abort the run — footer note in Results.md.
- No proprietary copyright headers in source files.

## Build & verify

```powershell
dotnet build src/GenAI.sln
```

Smoke test once implemented:

```powershell
$env:OPENAI_BASE_URL = "https://api.groq.com/openai/v1"; $env:OPENAI_API_KEY = "<key>"; $env:OPENAI_MODEL = "llama-3.3-70b-versatile"
dotnet run --project src/ResumeScorer -- <folder-with-resumes-and-JOB.md>
```

Verify `Results.md` appears with a sorted table and the console prints a summary line.

Keep `ScoreCalculator` pure C# (no I/O) so its outputs can be verified against known inputs.
