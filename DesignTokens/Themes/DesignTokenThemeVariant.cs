using Site.DesignTokens.Sources;

namespace Site.DesignTokens.Themes;

public sealed class DesignTokenThemeVariant
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Alias { get; init; }

    public required string Selector { get; init; }

    public required IReadOnlyList<DesignTokenSource> Sources { get; init; }

    public required bool IsDefault { get; init; }

    public required DesignTokenThemeVariantType VariantType { get; init; }

    public required bool Enabled { get; init; }
}
