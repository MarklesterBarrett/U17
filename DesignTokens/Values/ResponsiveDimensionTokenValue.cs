namespace Site.DesignTokens.Values;

public sealed class ResponsiveDimensionTokenValue
{
    public DimensionValue? Mobile { get; init; }

    public string? MobileReference { get; init; }

    public DimensionValue? Tablet { get; init; }

    public string? TabletReference { get; init; }

    public DimensionValue? Desktop { get; init; }

    public string? DesktopReference { get; init; }
}
