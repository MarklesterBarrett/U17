namespace Site.DesignTokens.Diagnostics;

public sealed class DesignTokenBuildReport
{
    public required bool Success { get; init; }

    public string? GeneratedCssPath { get; init; }

    public string? GeneratedTailwindPath { get; init; }

    public required int TokenCount { get; init; }

    public required int WarningCount { get; init; }

    public required int ErrorCount { get; init; }

    public int InfoCount { get; init; }

    public IReadOnlyList<DesignTokenPipelineStageResult> PipelineStages { get; init; } = [];

    public IReadOnlyList<DesignTokenDiagnostic> Infos { get; init; } = [];

    public IReadOnlyList<DesignTokenDiagnostic> Warnings { get; init; } = [];

    public IReadOnlyList<DesignTokenDiagnostic> Errors { get; init; } = [];
}
