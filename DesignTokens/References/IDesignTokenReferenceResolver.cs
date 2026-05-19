using Site.DesignTokens.Models;

namespace Site.DesignTokens.References;

public interface IDesignTokenReferenceResolver
{
    DesignTokenReferenceResolutionResult Resolve(DesignTokenRegistry registry);
}
