using Site.DesignTokens.Models;
using Site.DesignTokens.Parsing;
using Site.DesignTokens.Diagnostics;

namespace Site.DesignTokens.Sources;

public sealed class DesignTokenSourceMergeResult
{
    public DesignTokenSourceMergeResult(
        DesignTokenRegistry registry,
        IReadOnlyList<DesignTokenParseError> parseErrors,
        IReadOnlyList<DesignTokenSourceMergeError> errors,
        IReadOnlyList<DesignTokenSourceTrace> sourceTraces,
        IReadOnlyList<DesignTokenSourceSummary> sourceSummaries,
        IReadOnlyList<DesignTokenMergeEvent> mergeEvents,
        DesignTokenSourceSummaryTotals totals)
    {
        Registry = registry;
        ParseErrors = parseErrors;
        Errors = errors;
        SourceTraces = sourceTraces;
        SourceSummaries = sourceSummaries;
        MergeEvents = mergeEvents;
        Totals = totals;
    }

    public DesignTokenRegistry Registry { get; }

    public IReadOnlyList<DesignTokenParseError> ParseErrors { get; }

    public IReadOnlyList<DesignTokenSourceMergeError> Errors { get; }

    public IReadOnlyList<DesignTokenSourceTrace> SourceTraces { get; }

    public IReadOnlyList<DesignTokenSourceSummary> SourceSummaries { get; }

    public IReadOnlyList<DesignTokenMergeEvent> MergeEvents { get; }

    public DesignTokenSourceSummaryTotals Totals { get; }

    public bool Success => ParseErrors.Count == 0 && Errors.Count == 0;
}
