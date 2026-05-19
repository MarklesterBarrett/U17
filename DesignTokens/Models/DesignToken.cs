using Site.DesignTokens.Sources;

namespace Site.DesignTokens.Models;

public sealed class DesignToken
{
    public DesignToken(
        DesignTokenPath path,
        DesignTokenType type,
        object? rawValue = null,
        object? normalizedValue = null,
        object? resolvedValue = null,
        string? description = null,
        DesignTokenSourceType sourceType = DesignTokenSourceType.Imported,
        string? sourceName = null,
        int sourcePriority = DesignTokenSourcePriority.Imported,
        DesignTokenMetadata? metadata = null)
    {
        Path = path ?? throw new ArgumentNullException(nameof(path));
        Type = type;
        RawValue = rawValue;
        NormalizedValue = normalizedValue;
        ResolvedValue = resolvedValue;
        Description = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
        SourceType = sourceType;
        SourceName = string.IsNullOrWhiteSpace(sourceName)
            ? null
            : sourceName.Trim();
        SourcePriority = sourcePriority;
        Metadata = metadata;
    }

    public DesignToken(
        string path,
        DesignTokenType type,
        object? rawValue = null,
        object? normalizedValue = null,
        object? resolvedValue = null,
        string? description = null,
        DesignTokenSourceType sourceType = DesignTokenSourceType.Imported,
        string? sourceName = null,
        int sourcePriority = DesignTokenSourcePriority.Imported,
        DesignTokenMetadata? metadata = null)
        : this(new DesignTokenPath(path), type, rawValue, normalizedValue, resolvedValue, description, sourceType, sourceName, sourcePriority, metadata)
    {
    }

    public DesignTokenPath Path { get; }

    public string Name => Path.Name;

    public DesignTokenType Type { get; }

    public object? RawValue { get; }

    public object? NormalizedValue { get; }

    public object? ResolvedValue { get; }

    public object? Value => ResolvedValue ?? NormalizedValue ?? RawValue;

    public string? Description { get; }

    public DesignTokenSourceType SourceType { get; }

    public string? SourceName { get; }

    public int SourcePriority { get; }

    public DesignTokenMetadata? Metadata { get; }
}
