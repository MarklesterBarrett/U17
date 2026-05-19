namespace Site.DesignTokens.Diagnostics;

public sealed class DesignTokenDiagnostic
{
    public DesignTokenDiagnostic(
        DesignTokenDiagnosticStage stage,
        string message,
        string? tokenPath = null,
        string? field = null)
    {
        Stage = stage;
        Message = message ?? string.Empty;
        TokenPath = tokenPath;
        Field = field;
    }

    public DesignTokenDiagnosticStage Stage { get; }

    public string? TokenPath { get; }

    public string? Field { get; }

    public string Message { get; }
}
