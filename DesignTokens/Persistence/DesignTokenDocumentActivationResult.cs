using Site.DesignTokens.Diagnostics;

namespace Site.DesignTokens.Persistence;

public sealed class DesignTokenDocumentActivationResult
{
    public DesignTokenDocumentActivationResult(
        bool success,
        Guid? documentId,
        IReadOnlyList<DesignTokenDiagnostic> errors)
    {
        Success = success;
        DocumentId = documentId;
        Errors = errors;
    }

    public bool Success { get; }

    public Guid? DocumentId { get; }

    public IReadOnlyList<DesignTokenDiagnostic> Errors { get; }
}
