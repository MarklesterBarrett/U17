using Site.DesignTokens.Models;

namespace Site.DesignTokens.Validation;

public sealed class DesignTokenValidationResult
{
    public DesignTokenValidationResult(
        DesignTokenRegistry registry,
        IReadOnlyList<DesignTokenValidationError> errors)
    {
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
    }

    public DesignTokenRegistry Registry { get; }

    public IReadOnlyList<DesignTokenValidationError> Errors { get; }

    public bool Success => Errors.Count == 0;
}
