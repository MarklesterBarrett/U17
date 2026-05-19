namespace Site.DesignTokens.Management;

public sealed class DesignTokenPreviewComparisonItem
{
    public required string Path { get; init; }

    public string? ActiveValue { get; init; }

    public string? DraftValue { get; init; }

    public required string ChangeType { get; init; }
}
