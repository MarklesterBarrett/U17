namespace Site.DesignTokens.Management;

public sealed class DesignTokenManagementOptions
{
    public bool FailOnWarnings { get; init; }

    public bool EnableTailwindOutput { get; init; } = true;

    public string BuildStatusPath { get; init; } = Path.Combine("App_Data", "DesignTokens", "build-status.json");

    public int MaxPreviewJsonLength { get; init; } = 250_000;
}
