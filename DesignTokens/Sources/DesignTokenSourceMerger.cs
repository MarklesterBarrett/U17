using Site.DesignTokens.Diagnostics;
using Site.DesignTokens.Models;
using Site.DesignTokens.Parsing;

namespace Site.DesignTokens.Sources;

public sealed class DesignTokenSourceMerger : IDesignTokenSourceMerger
{
    private readonly IDesignTokenJsonParser _jsonParser;

    public DesignTokenSourceMerger(IDesignTokenJsonParser jsonParser)
    {
        _jsonParser = jsonParser;
    }

    public DesignTokenSourceMergeResult Merge(IEnumerable<DesignTokenSource> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        var sourceList = sources.Select((source, index) => new IndexedSource(source, index))
            .OrderBy(x => x.Source.Priority)
            .ThenBy(x => x.Index)
            .ToArray();

        var mergedTokens = new Dictionary<DesignTokenPath, DesignToken>();
        var sourceHistory = new Dictionary<DesignTokenPath, List<DesignTokenSourceDescriptor>>();
        var parseErrors = new List<DesignTokenParseError>();
        var errors = new List<DesignTokenSourceMergeError>();
        var mergeEvents = new List<DesignTokenMergeEvent>();
        var sourceTokenCounts = new Dictionary<(DesignTokenSourceType, string?, int), int>();
        var samePriorityDuplicates = 0;

        foreach (var item in sourceList)
        {
            var parseResult = _jsonParser.Parse(item.Source.Json);
            sourceTokenCounts[(item.Source.SourceType, item.Source.Name, item.Source.Priority)] = parseResult.Registry.All.Count;

            foreach (var parseError in parseResult.Errors)
            {
                parseErrors.Add(parseError);
            }

            foreach (var token in parseResult.Registry.All.OrderBy(x => x.Path.Value, StringComparer.Ordinal))
            {
                var sourceToken = new DesignToken(
                    token.Path,
                    token.Type,
                    rawValue: token.RawValue,
                    description: token.Description,
                    sourceType: item.Source.SourceType,
                    sourceName: item.Source.Name,
                    sourcePriority: item.Source.Priority);

                if (!sourceHistory.TryGetValue(sourceToken.Path, out var history))
                {
                    history = [];
                    sourceHistory[sourceToken.Path] = history;
                }

                history.Add(new DesignTokenSourceDescriptor(
                    item.Source.SourceType,
                    item.Source.Name,
                    item.Source.Priority,
                    sourceToken.Type.ToString(),
                    sourceToken.RawValue?.ToString()));

                if (!mergedTokens.TryGetValue(sourceToken.Path, out var existingToken))
                {
                    mergedTokens[sourceToken.Path] = sourceToken;
                    mergeEvents.Add(CreateEvent("Added", sourceToken.Path.Value, item.Source, $"Added {sourceToken.Path.Value} from {item.Source.SourceType}/{item.Source.Name ?? "Unnamed"}."));
                    continue;
                }

                if (sourceToken.SourcePriority > existingToken.SourcePriority)
                {
                    mergedTokens[sourceToken.Path] = sourceToken;
                    mergeEvents.Add(CreateEvent("Replaced", sourceToken.Path.Value, item.Source, $"Replaced {sourceToken.Path.Value} from {existingToken.SourceType}/{existingToken.SourceName ?? "Unnamed"} with {item.Source.SourceType}/{item.Source.Name ?? "Unnamed"}."));
                    continue;
                }

                if (sourceToken.SourcePriority == existingToken.SourcePriority)
                {
                    mergedTokens[sourceToken.Path] = sourceToken;
                    samePriorityDuplicates++;
                    mergeEvents.Add(CreateEvent("SamePriorityDuplicate", sourceToken.Path.Value, item.Source, $"Replaced {sourceToken.Path.Value} with same-priority source {item.Source.SourceType}/{item.Source.Name ?? "Unnamed"}."));
                    continue;
                }

                mergeEvents.Add(CreateEvent("Skipped", sourceToken.Path.Value, item.Source, $"Skipped {sourceToken.Path.Value} from {item.Source.SourceType}/{item.Source.Name ?? "Unnamed"} because higher-priority source already won."));
            }
        }

        var registry = new DesignTokenRegistry();
        foreach (var token in mergedTokens.Values.OrderBy(x => x.Path.Value, StringComparer.Ordinal))
        {
            registry.Add(token);
        }

        var sourceTraces = sourceHistory
            .OrderBy(x => x.Key.Value, StringComparer.Ordinal)
            .Select(x =>
            {
                var winning = x.Value[^1];
                var overridden = x.Value.Count > 1
                    ? x.Value.Take(x.Value.Count - 1).ToArray()
                    : [];

                return new DesignTokenSourceTrace(x.Key.Value, winning, overridden);
            })
            .ToArray();

        var sourceSummaries = sourceList
            .Select(item =>
            {
                var key = (item.Source.SourceType, item.Source.Name, item.Source.Priority);
                var tokenCountAfterMerge = sourceTraces.Count(trace =>
                    trace.WinningSource.SourceType == item.Source.SourceType &&
                    string.Equals(trace.WinningSource.SourceName, item.Source.Name, StringComparison.Ordinal) &&
                    trace.WinningSource.SourcePriority == item.Source.Priority);
                var overriddenByHigher = sourceTraces.Count(trace =>
                    trace.OverriddenSources.Any(source =>
                        source.SourceType == item.Source.SourceType &&
                        string.Equals(source.SourceName, item.Source.Name, StringComparison.Ordinal) &&
                        source.SourcePriority == item.Source.Priority) &&
                    !(trace.WinningSource.SourceType == item.Source.SourceType &&
                      string.Equals(trace.WinningSource.SourceName, item.Source.Name, StringComparison.Ordinal) &&
                      trace.WinningSource.SourcePriority == item.Source.Priority));

                return new DesignTokenSourceSummary
                {
                    SourceType = item.Source.SourceType,
                    SourceName = item.Source.Name,
                    Priority = item.Source.Priority,
                    Enabled = true,
                    TokenCountBeforeMerge = sourceTokenCounts.GetValueOrDefault(key, 0),
                    TokenCountAfterMerge = tokenCountAfterMerge,
                    TokensOverriddenByHigherSource = overriddenByHigher
                };
            })
            .ToArray();

        var totals = new DesignTokenSourceSummaryTotals
        {
            TotalTokensBeforeMerge = sourceSummaries.Sum(x => x.TokenCountBeforeMerge),
            TotalTokensAfterMerge = registry.All.Count,
            TokensAdded = mergeEvents.Count(x => string.Equals(x.EventType, "Added", StringComparison.Ordinal)),
            TokensOverridden = mergeEvents.Count(x => string.Equals(x.EventType, "Replaced", StringComparison.Ordinal)),
            SamePriorityDuplicates = samePriorityDuplicates,
            DisabledSources = 0
        };

        return new DesignTokenSourceMergeResult(registry, parseErrors, errors, sourceTraces, sourceSummaries, mergeEvents, totals);
    }

    private static DesignTokenMergeEvent CreateEvent(string eventType, string tokenPath, DesignTokenSource source, string message) => new()
    {
        EventType = eventType,
        TokenPath = tokenPath,
        SourceType = source.SourceType.ToString(),
        SourceName = source.Name,
        SourcePriority = source.Priority,
        Message = message
    };

    private sealed record IndexedSource(DesignTokenSource Source, int Index);
}
