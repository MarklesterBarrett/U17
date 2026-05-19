using Site.DesignTokens.Diagnostics;

namespace Site.DesignTokens;

public sealed class DesignTokenCssGenerator : IDesignTokenCssGenerator
{
    private readonly IDesignTokenDiagnosticsService _diagnosticsService;

    public DesignTokenCssGenerator(IDesignTokenDiagnosticsService diagnosticsService)
    {
        _diagnosticsService = diagnosticsService;
    }

    public string GenerateCss(Guid tenantKey) => _diagnosticsService.Inspect(null).Css;
}
