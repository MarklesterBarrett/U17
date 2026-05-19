namespace Site.DesignTokens.Management;

public sealed class DesignTokenPreviewComparisonSummary
{
    public int ChangedTokenCount { get; init; }

    public int AddedTokenCount { get; init; }

    public int RemovedTokenCount { get; init; }

    public IReadOnlyList<DesignTokenPreviewComparisonItem> ChangedTokens { get; init; } = [];

    public IReadOnlyList<DesignTokenPreviewComparisonItem> AddedTokens { get; init; } = [];

    public IReadOnlyList<DesignTokenPreviewComparisonItem> RemovedTokens { get; init; } = [];
}
