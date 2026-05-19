namespace Site.DesignTokens.Diagnostics;

public interface IDesignTokenDiagnosticsService
{
    DesignTokenDiagnosticsResult Inspect(string? importedJson = null);

    IReadOnlyList<DesignTokenDiagnosticViewModel> GetTokens(string? importedJson = null);

    DesignTokenDiagnosticViewModel? GetToken(string path, string? importedJson = null);

    IReadOnlyList<Site.DesignTokens.Sources.DesignTokenSourceTrace> GetSources(string? importedJson = null);

    IReadOnlyList<DesignTokenDependencyNode> GetDependencies(string? importedJson = null);

    IReadOnlyList<DesignTokenDiagnostic> GetErrors(string? importedJson = null);
}
