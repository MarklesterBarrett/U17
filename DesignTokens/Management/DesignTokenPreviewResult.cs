using Site.DesignTokens.Diagnostics;
using Site.DesignTokens.Themes;

namespace Site.DesignTokens.Management;

public sealed class DesignTokenPreviewResult
{
    public required bool Success { get; init; }

    public string PreviewCss { get; init; } = string.Empty;

    public string TailwindJson { get; init; } = string.Empty;

    public int TokenCount { get; init; }

    public IReadOnlyList<DesignTokenDiagnosticViewModel> Tokens { get; init; } = [];

    public IReadOnlyList<DesignTokenDiagnostic> Errors { get; init; } = [];

    public IReadOnlyList<DesignTokenDiagnostic> Warnings { get; init; } = [];

    public DesignTokenBuildReport? BuildReport { get; init; }

    public DesignTokenPreviewComparisonSummary Comparison { get; init; } = new();

    public IReadOnlyList<DesignTokenThemeVariantSummary> ThemeVariants { get; init; } = [];

    public IReadOnlyList<DesignTokenSourceSummary> SourceSummaries { get; init; } = [];

    public DesignTokenSourceSummaryTotals SourceSummaryTotals { get; init; } = new();

    public IReadOnlyList<DesignTokenMergeEvent> MergeEvents { get; init; } = [];

    public object? DiagnosticsExport { get; init; }
}
