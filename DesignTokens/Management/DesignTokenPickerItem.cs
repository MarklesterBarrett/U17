namespace Site.DesignTokens.Management;

public sealed class DesignTokenPickerItem
{
    public required string Path { get; init; }

    public required string Type { get; init; }

    public required string Label { get; init; }

    public required string SourceType { get; init; }

    public string? SourceName { get; init; }

    public string? ResolvedValuePreview { get; init; }

    public string? CssVariableName { get; init; }

    public required string ReferenceValue { get; init; }
}
