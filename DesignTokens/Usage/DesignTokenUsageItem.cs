namespace Site.DesignTokens.Usage;

public sealed class DesignTokenUsageItem
{
    public required DesignTokenUsageKind Kind { get; init; }

    public string? TokenPath { get; init; }

    public string? CssVariableName { get; init; }

    public string? FilePath { get; init; }

    public int? LineNumber { get; init; }

    public required string Message { get; init; }
}
