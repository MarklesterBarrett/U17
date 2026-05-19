namespace Site.DesignTokens.Sources;

public sealed class DesignTokenSourceDescriptor
{
    public DesignTokenSourceDescriptor(
        DesignTokenSourceType sourceType,
        string? sourceName,
        int sourcePriority,
        string? tokenType = null,
        string? rawValue = null)
    {
        SourceType = sourceType;
        SourceName = sourceName;
        SourcePriority = sourcePriority;
        TokenType = tokenType;
        RawValue = rawValue;
    }

    public DesignTokenSourceType SourceType { get; }

    public string? SourceName { get; }

    public int SourcePriority { get; }

    public string? TokenType { get; }

    public string? RawValue { get; }
}
