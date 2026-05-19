using Site.DesignTokens.Models;

namespace Site.DesignTokens.References;

public sealed class DesignTokenReferenceResolutionResult
{
    public DesignTokenReferenceResolutionResult(
        DesignTokenRegistry registry,
        IReadOnlyList<DesignTokenReferenceResolutionError> errors)
    {
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
    }

    public DesignTokenRegistry Registry { get; }

    public IReadOnlyList<DesignTokenReferenceResolutionError> Errors { get; }

    public bool Success => Errors.Count == 0;
}
