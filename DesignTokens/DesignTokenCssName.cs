namespace Site.DesignTokens;

internal static class DesignTokenCssName
{
    public static string FromAlias(string alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            return string.Empty;
        }

        return string.Join(
            "-",
            alias
                .Trim()
                .Replace('.', '-')
                .Split([' ', '_', '-'], StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant();
    }
}
