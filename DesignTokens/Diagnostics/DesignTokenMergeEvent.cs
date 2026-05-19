namespace Site.DesignTokens.Diagnostics;

public sealed class DesignTokenMergeEvent
{
    public required string EventType { get; init; }

    public required string Message { get; init; }

    public string? TokenPath { get; init; }

    public string? SourceType { get; init; }

    public string? SourceName { get; init; }

    public int? SourcePriority { get; init; }

    public bool IsInfo { get; init; } = true;
}
