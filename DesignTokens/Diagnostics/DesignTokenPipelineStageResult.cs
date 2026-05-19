namespace Site.DesignTokens.Diagnostics;

public sealed class DesignTokenPipelineStageResult
{
    public required DesignTokenDiagnosticStage Stage { get; init; }

    public required bool Success { get; init; }

    public IReadOnlyList<DesignTokenDiagnostic> Errors { get; init; } = [];

    public IReadOnlyList<DesignTokenDiagnostic> Warnings { get; init; } = [];

    public required long DurationMs { get; init; }
}
