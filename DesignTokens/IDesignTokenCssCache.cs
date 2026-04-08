namespace Site.DesignTokens;

public interface IDesignTokenCssCache
{
    string GetCss();
    void Invalidate();
}
