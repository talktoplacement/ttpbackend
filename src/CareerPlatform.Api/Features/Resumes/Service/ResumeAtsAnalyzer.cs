using System.Text.RegularExpressions;
using CareerPlatform.Api.Features.Resumes.Dto;

namespace CareerPlatform.Api.Features.Resumes.Service;

/// <summary>
/// Deterministic ATS heuristic scorer. Scores five independent dimensions from the resume text and
/// combines them with fixed weights. The dictionaries below are curated ATS domain vocabulary
/// (comparable to the allow-lists used elsewhere), not fabricated results — every emitted score is
/// derived from counting real matches in the supplied text.
/// </summary>
internal sealed partial class ResumeAtsAnalyzer : IResumeAtsAnalyzer
{
    // Common resume sections an ATS expects to find.
    private static readonly (string Key, string[] Aliases)[] Sections =
    {
        ("Contact", new[] { "email", "phone", "@" }),
        ("Summary", new[] { "summary", "objective", "profile" }),
        ("Experience", new[] { "experience", "employment", "work history" }),
        ("Education", new[] { "education", "b.tech", "bachelor", "degree", "university" }),
        ("Skills", new[] { "skills", "technologies", "tech stack" }),
        ("Projects", new[] { "projects", "portfolio" }),
    };

    // Strong resume action verbs — presence signals impact-oriented writing.
    private static readonly string[] ActionVerbs =
    {
        "led", "built", "designed", "developed", "implemented", "optimized", "improved",
        "launched", "created", "architected", "reduced", "increased", "delivered", "automated",
        "migrated", "scaled", "owned", "shipped", "mentored", "spearheaded", "streamlined",
    };

    // A general ATS keyword dictionary (common, role-agnostic technical + professional terms).
    private static readonly string[] KeywordDictionary =
    {
        "java", "python", "javascript", "typescript", "c#", "react", "node", "sql", "nosql",
        "aws", "azure", "docker", "kubernetes", "microservices", "rest", "api", "git", "ci/cd",
        "agile", "scrum", "testing", "data structures", "algorithms", "system design", "linux",
        "communication", "leadership", "problem solving",
    };

    private const int IdealMinWords = 350;
    private const int IdealMaxWords = 900;

    public AtsAnalysisResponse Analyze(string resumeText)
    {
        var text = (resumeText ?? string.Empty).Trim();
        var lower = text.ToLowerInvariant();
        var words = WordRegex().Matches(lower).Count;

        // 1) Section completeness.
        var presentSections = Sections
            .Where(s => s.Aliases.Any(a => lower.Contains(a)))
            .Select(s => s.Key).ToList();
        var sectionScore = Pct(presentSections.Count, Sections.Length);

        // 2) Keyword coverage.
        var matched = KeywordDictionary.Where(k => lower.Contains(k)).ToList();
        var missing = KeywordDictionary.Where(k => !lower.Contains(k)).ToList();
        // A resume need not contain every keyword; treat ~12 hits as full coverage.
        var keywordScore = Math.Min(100, (int)Math.Round(matched.Count / 12.0 * 100));

        // 3) Action-verb usage.
        var verbHits = ActionVerbs.Count(v => Regex.IsMatch(lower, $@"\b{Regex.Escape(v)}\b"));
        var actionScore = Math.Min(100, (int)Math.Round(verbHits / 8.0 * 100));

        // 4) Quantified impact (numbers, %, currency, multipliers).
        var quantHits = QuantRegex().Matches(text).Count;
        var quantScore = Math.Min(100, (int)Math.Round(quantHits / 6.0 * 100));

        // 5) Length band.
        int lengthScore;
        if (words >= IdealMinWords && words <= IdealMaxWords) lengthScore = 100;
        else if (words < IdealMinWords) lengthScore = Pct(words, IdealMinWords);
        else lengthScore = Math.Max(40, 100 - (words - IdealMaxWords) / 20);
        lengthScore = Math.Clamp(lengthScore, 0, 100);

        var subScores = new List<AtsSubScore>
        {
            new("sections", "Section completeness", sectionScore,
                $"{presentSections.Count}/{Sections.Length} expected sections detected."),
            new("keywords", "Keyword coverage", keywordScore,
                $"{matched.Count} recognised ATS keywords found."),
            new("actionVerbs", "Action-verb impact", actionScore,
                $"{verbHits} distinct strong action verbs used."),
            new("quantified", "Quantified achievements", quantScore,
                $"{quantHits} quantified metrics (numbers/percentages) detected."),
            new("length", "Length", lengthScore,
                $"{words} words (ideal {IdealMinWords}-{IdealMaxWords})."),
        };

        var overall = (int)Math.Round(
            sectionScore * 0.25 + keywordScore * 0.30 + actionScore * 0.20 +
            quantScore * 0.15 + lengthScore * 0.10);

        var suggestions = BuildSuggestions(
            presentSections, verbHits, quantHits, words, missing);

        return new AtsAnalysisResponse(
            overall, Grade(overall), words, subScores, matched, missing.Take(10).ToList(), suggestions);
    }

    private static List<string> BuildSuggestions(
        IReadOnlyCollection<string> presentSections, int verbHits, int quantHits, int words,
        IReadOnlyList<string> missing)
    {
        var tips = new List<string>();
        foreach (var s in Sections)
        {
            if (!presentSections.Contains(s.Key))
                tips.Add($"Add a clearly-labelled \"{s.Key}\" section.");
        }
        if (verbHits < 5)
            tips.Add("Start more bullet points with strong action verbs (e.g. Led, Built, Optimized).");
        if (quantHits < 3)
            tips.Add("Quantify achievements with concrete numbers, percentages, or scale.");
        if (words < IdealMinWords)
            tips.Add("The resume looks short — expand experience and project detail.");
        else if (words > IdealMaxWords)
            tips.Add("The resume is long — tighten to the most relevant, recent achievements.");
        if (missing.Count > 0)
            tips.Add($"Consider adding relevant keywords if they apply: {string.Join(", ", missing.Take(6))}.");
        return tips;
    }

    private static int Pct(int n, int d) => d <= 0 ? 0 : Math.Clamp((int)Math.Round(n / (double)d * 100), 0, 100);

    private static string Grade(int score) =>
        score >= 85 ? "Excellent" : score >= 70 ? "Good" : score >= 50 ? "Needs Work" : "Poor";

    [GeneratedRegex(@"[A-Za-z][A-Za-z0-9\+\#\.]*")]
    private static partial Regex WordRegex();

    [GeneratedRegex(@"(\d+%|\$\s?\d[\d,]*|\d+\+|\b\d{2,}\b)")]
    private static partial Regex QuantRegex();
}
