namespace Site.DesignTokens.Diagnostics;

public enum DesignTokenDiagnosticStage
{
    SourceMerge,
    Parse,
    Normalise,
    Resolve,
    Validate,
    CssGenerate,
    TailwindGenerate,
    CssWrite,
    TailwindWrite
}
