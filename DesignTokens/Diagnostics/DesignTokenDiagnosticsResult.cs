using Site.DesignTokens.Models;
using Site.DesignTokens.Sources;
using Site.DesignTokens.Themes;
using Site.DesignTokens.Usage;

namespace Site.DesignTokens.Diagnostics;

public sealed class DesignTokenDiagnosticsResult
{
    public required DesignTokenRegistry Registry { get; init; }

    public required DesignTokenBuildReport BuildReport { get; init; }

    public IReadOnlyList<DesignTokenDiagnosticViewModel> Tokens { get; init; } = [];

    public IReadOnlyList<DesignTokenDependencyNode> Dependencies { get; init; } = [];

    public IReadOnlyList<DesignTokenSourceTrace> SourceTraces { get; init; } = [];

    public IReadOnlyList<IReadOnlyList<string>> CircularReferenceChains { get; init; } = [];

    public IReadOnlyDictionary<string, string> DebugExports { get; init; } = new Dictionary<string, string>();

    public string Css { get; init; } = string.Empty;

    public string TailwindJson { get; init; } = string.Empty;

    public IReadOnlyList<DesignTokenThemeVariantSummary> ThemeVariants { get; init; } = [];

    public DesignTokenUsageScanResult? UsageScan { get; init; }

    public IReadOnlyList<DesignTokenSourceSummary> SourceSummaries { get; init; } = [];

    public DesignTokenSourceSummaryTotals SourceSummaryTotals { get; init; } = new();

    public IReadOnlyList<DesignTokenMergeEvent> MergeEvents { get; init; } = [];

    public DesignTokenDiagnosticsExport DiagnosticsExport { get; init; } = new();
}
