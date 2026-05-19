using Site.DesignTokens.Diagnostics;

namespace Site.DesignTokens.Import;

public sealed class DesignTokenImportResult
{
    public DesignTokenImportResult(
        bool success,
        Guid? documentId,
        IReadOnlyList<DesignTokenDiagnostic> errors,
        IReadOnlyList<string> warnings,
        int parsedTokenCount,
        string? generatedCssPreview)
    {
        Success = success;
        DocumentId = documentId;
        Errors = errors;
        Warnings = warnings;
        ParsedTokenCount = parsedTokenCount;
        GeneratedCssPreview = generatedCssPreview;
    }

    public bool Success { get; }

    public Guid? DocumentId { get; }

    public IReadOnlyList<DesignTokenDiagnostic> Errors { get; }

    public IReadOnlyList<string> Warnings { get; }

    public int ParsedTokenCount { get; }

    public string? GeneratedCssPreview { get; }
}
