using Site.DesignTokens.Sources;

namespace Site.DesignTokens.Diagnostics;

public sealed class DesignTokenSourceSummary
{
    public required DesignTokenSourceType SourceType { get; init; }

    public string? SourceName { get; init; }

    public required int Priority { get; init; }

    public required bool Enabled { get; init; }

    public int TokenCountBeforeMerge { get; init; }

    public int TokenCountAfterMerge { get; init; }

    public int TokensOverriddenByHigherSource { get; init; }

    public int ErrorCount { get; init; }

    public int WarningCount { get; init; }

    public int InfoCount { get; init; }
}
