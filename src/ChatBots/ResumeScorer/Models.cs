using System.Collections.Generic;

namespace ChatBots.ResumeScorer;

record CandidateFields(
    string? Name,
    int? GraduationYear,
    string? University,
    double? Cgpa);

record CandidateResult(
    string FileName,
    CandidateFields Fields,
    double Score,
    string Recommendation);

record ResumeEvaluation(
    CandidateFields Fields,
    ScoreBreakdown Breakdown);

record MatchedSection(
    string Requirement,
    string Evidence);

record ScoreBreakdown(
    List<string> MatchedSkills,
    List<string> MissingSkills,
    List<MatchedSection> MatchedSections,
    List<string> MissingRequirements,
    double HolisticAtsScore);
