namespace Site.DesignTokens.Diagnostics;

public sealed class DesignTokenDiagnosticsOptions
{
    public bool EnableDebugExports { get; init; }

    public string OutputDirectory { get; init; } = Path.Combine("App_Data", "DesignTokens", "Diagnostics");

    public bool EnableUsageScanning { get; init; }
}
