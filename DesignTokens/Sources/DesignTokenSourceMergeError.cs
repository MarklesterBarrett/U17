namespace Site.DesignTokens.Sources;

public sealed class DesignTokenSourceMergeError
{
    public DesignTokenSourceMergeError(
        DesignTokenSourceType sourceType,
        string? sourceName,
        string path,
        string message)
    {
        SourceType = sourceType;
        SourceName = sourceName;
        Path = path ?? string.Empty;
        Message = message ?? string.Empty;
    }

    public DesignTokenSourceType SourceType { get; }

    public string? SourceName { get; }

    public string Path { get; }

    public string Message { get; }
}
