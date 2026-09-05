using CareerPlatform.Api.Features.Resumes.Dto;

namespace CareerPlatform.Api.Features.Resumes.Service;

/// <summary>
/// Pure, deterministic ATS scorer. Given the plain text of a resume it returns a reproducible
/// score breakdown — no I/O, no randomness — so it is trivially unit-testable and stable.
/// </summary>
public interface IResumeAtsAnalyzer
{
    AtsAnalysisResponse Analyze(string resumeText);
}
