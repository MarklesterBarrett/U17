namespace Site.DesignTokens;

public interface IDesignTokenStyleRenderer
{
    string? ResolveColorDeclaration(string cssProperty, string? tokenAlias);
    string? CombineDeclarations(params string?[] declarations);
}
