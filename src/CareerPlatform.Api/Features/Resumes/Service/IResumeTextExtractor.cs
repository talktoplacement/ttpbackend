namespace CareerPlatform.Api.Features.Resumes.Service;

/// <summary>
/// Extracts plain text from a resume blob so the ATS analyzer can score it. Supports UTF-8
/// text/markdown content and best-effort extraction from (uncompressed or Flate-compressed) PDFs.
/// Image-only / scanned PDFs yield little or no text — callers must handle the empty case rather
/// than treat it as a valid low score.
/// </summary>
public interface IResumeTextExtractor
{
    /// <summary>Returns the extracted plain text (may be empty when nothing is machine-readable).</summary>
    string ExtractText(byte[] content);
}
