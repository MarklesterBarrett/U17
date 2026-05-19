using Site.DesignTokens.Models;

namespace Site.DesignTokens.Parsing;

public sealed class DesignTokenParseResult
{
    public DesignTokenParseResult(DesignTokenRegistry registry, IReadOnlyList<DesignTokenParseError> errors)
    {
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
    }

    public DesignTokenRegistry Registry { get; }

    public IReadOnlyList<DesignTokenParseError> Errors { get; }

    public bool Success => Errors.Count == 0;
}
