using Site.DesignTokens.Models;

namespace Site.DesignTokens.Css;

public interface IDesignTokenCssGenerator
{
    DesignTokenCssGenerationResult Generate(DesignTokenRegistry registry, string selector = ":root", bool includeHeader = true);
}
