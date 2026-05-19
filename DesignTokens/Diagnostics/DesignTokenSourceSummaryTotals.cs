namespace Site.DesignTokens.Diagnostics;

public sealed class DesignTokenSourceSummaryTotals
{
    public int TotalTokensBeforeMerge { get; init; }

    public int TotalTokensAfterMerge { get; init; }

    public int TokensAdded { get; init; }

    public int TokensOverridden { get; init; }

    public int SamePriorityDuplicates { get; init; }

    public int DisabledSources { get; init; }
}
