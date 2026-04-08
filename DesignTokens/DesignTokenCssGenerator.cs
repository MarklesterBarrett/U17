using System.Text;

namespace Site.DesignTokens;

public sealed class DesignTokenCssGenerator : IDesignTokenCssGenerator
{
    private readonly IDesignTokenProvider _designTokenProvider;

    public DesignTokenCssGenerator(IDesignTokenProvider designTokenProvider)
    {
        _designTokenProvider = designTokenProvider;
    }

    public string GenerateCss()
    {
        var tokens = _designTokenProvider.GetTokens();
        var builder = new StringBuilder();

        builder.AppendLine("/* Generated from site settings. Do not edit manually. */");
        builder.AppendLine(":root {");

        foreach (var color in tokens.Colors.OrderBy(x => x.Alias, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append("  --")
                .Append(color.Alias)
                .Append(": ")
                .Append(color.Value)
                .AppendLine(";");
        }

        foreach (var spacing in tokens.Spacing.OrderBy(x => x.Alias, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append("  --")
                .Append(spacing.Alias)
                .Append(": ")
                .Append(spacing.Mobile)
                .AppendLine(";");
        }

        foreach (var value in tokens.Values.OrderBy(x => x.Alias, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append("  --")
                .Append(value.Alias)
                .Append(": ")
                .Append(value.Value)
                .AppendLine(";");
        }

        builder.AppendLine("}");
        AppendSpacingBreakpoint(builder, "48rem", tokens.Spacing, x => x.Tablet);
        AppendSpacingBreakpoint(builder, "64rem", tokens.Spacing, x => x.Laptop);
        AppendSpacingBreakpoint(builder, "80rem", tokens.Spacing, x => x.Desktop);

        return builder.ToString();
    }

    private static void AppendSpacingBreakpoint(
        StringBuilder builder,
        string minWidth,
        IReadOnlyList<SpacingTokenDefinition> tokens,
        Func<SpacingTokenDefinition, string> valueSelector)
    {
        builder.Append("@media (min-width: ")
            .Append(minWidth)
            .AppendLine(") {");
        builder.AppendLine("  :root {");

        foreach (var spacing in tokens.OrderBy(x => x.Alias, StringComparer.OrdinalIgnoreCase))
        {
            builder.Append("    --")
                .Append(spacing.Alias)
                .Append(": ")
                .Append(valueSelector(spacing))
                .AppendLine(";");
        }

        builder.AppendLine("  }");
        builder.AppendLine("}");
    }
}
