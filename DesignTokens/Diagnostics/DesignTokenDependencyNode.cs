using Site.DesignTokens.Sources;

namespace Site.DesignTokens.Diagnostics;

public sealed class DesignTokenDependencyNode
{
    public required string TokenPath { get; init; }

    public IReadOnlyList<string> IncomingReferences { get; init; } = [];

    public IReadOnlyList<string> OutgoingReferences { get; init; } = [];

    public IReadOnlyList<string> UnresolvedReferences { get; init; } = [];

    public DesignTokenSourceType SourceType { get; init; }

    public string? SourceName { get; init; }
}
