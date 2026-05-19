namespace Site.DesignTokens.Models;

public sealed class DesignTokenMetadata
{
    public string? Category { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = [];

    public bool Deprecated { get; init; }

    public string? ReplacementToken { get; init; }

    public string? Notes { get; init; }
}
