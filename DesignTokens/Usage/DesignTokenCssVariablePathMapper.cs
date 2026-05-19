using Site.DesignTokens.Css;
using Site.DesignTokens.Models;

namespace Site.DesignTokens.Usage;

public sealed class DesignTokenCssVariablePathMapper
{
    public bool TryMap(string cssVariableName, DesignTokenRegistry registry, out string tokenPath)
    {
        tokenPath = string.Empty;

        if (string.IsNullOrWhiteSpace(cssVariableName))
        {
            return false;
        }

        foreach (var token in registry.All)
        {
            if (!DesignTokenCssVariableName.TryCreate(token, out var generatedName, out _))
            {
                continue;
            }

            if (string.Equals(generatedName, cssVariableName, StringComparison.Ordinal))
            {
                tokenPath = token.Path.Value;
                return true;
            }
        }

        return false;
    }
}
