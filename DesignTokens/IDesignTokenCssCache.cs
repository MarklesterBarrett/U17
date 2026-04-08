namespace Site.DesignTokens;

public interface IDesignTokenCssCache
{
    string GetCss(Guid tenantKey);
    void Invalidate();
}
