using Site.DesignTokens.Models;

namespace Site.DesignTokens.Normalization;

public interface IDesignTokenValueNormalizer
{
    DesignTokenNormalizationResult Normalize(DesignTokenRegistry registry);
}
