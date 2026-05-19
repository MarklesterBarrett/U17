using System.Collections.Immutable;

namespace Site.DesignTokens.Models;

public sealed class DesignTokenPath : IEquatable<DesignTokenPath>
{
    private readonly ImmutableArray<string> _segments;

    public DesignTokenPath(IEnumerable<string> segments)
    {
        ArgumentNullException.ThrowIfNull(segments);

        _segments = segments
            .Select(NormalizeSegment)
            .ToImmutableArray();

        if (_segments.IsDefaultOrEmpty)
        {
            throw new ArgumentException("Token path must contain at least one segment.", nameof(segments));
        }

        Value = string.Join(".", _segments);
        Name = _segments[^1];
    }

    public DesignTokenPath(string path)
        : this(SplitPath(path))
    {
    }

    public string Value { get; }

    public string Name { get; }

    public IReadOnlyList<string> Segments => _segments;

    public override string ToString() => Value;

    public bool Equals(DesignTokenPath? other) =>
        other is not null &&
        string.Equals(Value, other.Value, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as DesignTokenPath);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

    public static bool operator ==(DesignTokenPath? left, DesignTokenPath? right) =>
        Equals(left, right);

    public static bool operator !=(DesignTokenPath? left, DesignTokenPath? right) =>
        !Equals(left, right);

    private static IEnumerable<string> SplitPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return path.Split('.');
    }

    private static string NormalizeSegment(string segment)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(segment);

        var normalized = segment.Trim();
        if (normalized.Length == 0)
        {
            throw new ArgumentException("Token path segments cannot be empty.", nameof(segment));
        }

        return normalized;
    }
}
