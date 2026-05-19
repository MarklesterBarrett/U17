namespace Site.DesignTokens.Themes;

public sealed class DesignTokenThemeVariantSummary
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Alias { get; init; }

    public required string Selector { get; init; }

    public required bool IsDefault { get; init; }

    public required string VariantType { get; init; }

    public required bool Enabled { get; init; }
}
