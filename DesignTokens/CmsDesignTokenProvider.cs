using Site.DesignTokens.Diagnostics;

namespace Site.DesignTokens;

public sealed class CmsDesignTokenProvider : IDesignTokenProvider
{
    private readonly IDesignTokenDiagnosticsService _diagnosticsService;

    public CmsDesignTokenProvider(IDesignTokenDiagnosticsService diagnosticsService)
    {
        _diagnosticsService = diagnosticsService;
    }

    public DesignTokenSet GetTokens() => Map(_diagnosticsService.GetTokens());

    public DesignTokenSet GetTokens(Guid tenantKey) => GetTokens();

    private static DesignTokenSet Map(IReadOnlyList<DesignTokenDiagnosticViewModel> tokens)
    {
        var colors = tokens
            .Where(x => string.Equals(x.Type, "Color", StringComparison.Ordinal))
            .Select(x => new ColorTokenDefinition(
                x.Path,
                x.Path,
                x.ResolvedValue ?? x.GeneratedCssValue ?? x.NormalizedValue ?? x.RawValue ?? string.Empty))
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .ToArray();

        var spacing = tokens
            .Where(IsSpacingToken)
            .Select(x =>
            {
                var preview = x.GeneratedCssValue ?? x.ResolvedValue ?? x.NormalizedValue ?? x.RawValue ?? string.Empty;
                return new SpacingTokenDefinition(x.Path, x.Path, preview, preview, preview);
            })
            .Where(x => !string.IsNullOrWhiteSpace(x.Mobile))
            .ToArray();

        var values = tokens
            .Where(x => !string.Equals(x.Type, "Color", StringComparison.Ordinal) && !IsSpacingToken(x))
            .Select(x => new ValueTokenDefinition(
                x.Path,
                x.Path,
                x.GeneratedCssValue ?? x.ResolvedValue ?? x.NormalizedValue ?? x.RawValue ?? string.Empty))
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .ToArray();

        return new DesignTokenSet(colors, spacing, values);
    }

    private static bool IsSpacingToken(DesignTokenDiagnosticViewModel token)
    {
        if (!string.Equals(token.Type, "Dimension", StringComparison.Ordinal))
        {
            return false;
        }

        return token.Path.StartsWith("space.", StringComparison.Ordinal) ||
               token.Path.StartsWith("spacing.", StringComparison.Ordinal);
    }
}
