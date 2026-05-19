using Site.DesignTokens.Models;

namespace Site.DesignTokens.Normalization;

public sealed class DesignTokenNormalizationResult
{
    public DesignTokenNormalizationResult(
        DesignTokenRegistry registry,
        IReadOnlyList<DesignTokenNormalizationError> errors)
    {
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
    }

    public DesignTokenRegistry Registry { get; }

    public IReadOnlyList<DesignTokenNormalizationError> Errors { get; }

    public bool Success => Errors.Count == 0;
}
