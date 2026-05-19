namespace Site.DesignTokens.Usage;

public sealed class DesignTokenUsageScanResult
{
    public string RootPath { get; init; } = string.Empty;

    public IReadOnlyList<DesignTokenUsageItem> Items { get; init; } = [];

    public IReadOnlyList<string> UsedTokenPaths { get; init; } = [];

    public IReadOnlyList<string> UnusedTokenPaths { get; init; } = [];

    public int ScannedFileCount { get; init; }

    public bool Enabled { get; init; }
}
