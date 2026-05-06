namespace Site.DesignTokens;

public sealed record DesignTokenSet(
    IReadOnlyList<ColorTokenDefinition> Colors,
    IReadOnlyList<SpacingTokenDefinition> Spacing,
    IReadOnlyList<ValueTokenDefinition> Values);

public sealed record ColorTokenDefinition(
    string Alias,
    string Label,
    string Value);

public sealed record SpacingTokenDefinition(
    string Alias,
    string Label,
    string Mobile,
    string Tablet,
    string Desktop);

public sealed record ValueTokenDefinition(
    string Alias,
    string Label,
    string Value,
    string Tablet = "",
    string Desktop = "");
