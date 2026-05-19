namespace Site.DesignTokens.Management;

public static class DesignTokenPickerContext
{
    public static string? ResolveTokenType(string? context)
    {
        if (string.IsNullOrWhiteSpace(context))
        {
            return null;
        }

        return Normalize(context) switch
        {
            "color" => "color",
            "dimension" => "dimension",
            "fontfamily" => "fontfamily",
            "fontweight" => "fontweight",
            "duration" => "duration",
            "number" => "number",
            "typography.fontfamily" => "fontfamily",
            "typography.fontweight" => "fontweight",
            "typography.fontsize" => "dimension",
            "typography.lineheight" => "number",
            "typography.letterspacing" => "dimension",
            "border.width" => "dimension",
            "border.color" => "color",
            "shadow.color" => "color",
            "shadow.offsetx" => "dimension",
            "shadow.offsety" => "dimension",
            "shadow.blur" => "dimension",
            "shadow.spread" => "dimension",
            _ => null
        };
    }

    public static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : value.Replace(" ", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
}
