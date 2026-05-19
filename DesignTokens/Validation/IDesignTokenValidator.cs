using Site.DesignTokens.Models;

namespace Site.DesignTokens.Validation;

public interface IDesignTokenValidator
{
    DesignTokenValidationResult Validate(DesignTokenRegistry registry);
}
