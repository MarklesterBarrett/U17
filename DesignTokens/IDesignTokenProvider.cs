namespace Site.DesignTokens;

public interface IDesignTokenProvider
{
    DesignTokenSet GetTokens();
    DesignTokenSet GetTokens(Guid tenantKey);
}
