namespace Site.DesignTokens.Values;

public sealed class DimensionTokenValue
{
    public decimal? Value { get; init; }

    public string Unit { get; init; } = string.Empty;

    public string? Reference { get; init; }
}
