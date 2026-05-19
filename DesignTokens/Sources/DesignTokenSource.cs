namespace Site.DesignTokens.Sources;

public sealed class DesignTokenSource
{
    public DesignTokenSource(
        DesignTokenSourceType sourceType,
        string? name,
        string json,
        int? priority = null)
    {
        SourceType = sourceType;
        Name = string.IsNullOrWhiteSpace(name)
            ? null
            : name.Trim();
        Json = json ?? string.Empty;
        Priority = priority ?? DesignTokenSourcePriority.GetDefault(sourceType);
    }

    public DesignTokenSourceType SourceType { get; }

    public string? Name { get; }

    public string Json { get; }

    public int Priority { get; }
}
