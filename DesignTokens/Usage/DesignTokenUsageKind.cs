namespace Site.DesignTokens.Usage;

public enum DesignTokenUsageKind
{
    TokenReferenceUsed,
    CssVariableUsed,
    MissingTokenReference,
    MissingGeneratedCssVariable,
    HardcodedDesignValue,
    UnusedGeneratedToken
}
