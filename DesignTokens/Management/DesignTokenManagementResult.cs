using Site.DesignTokens.Diagnostics;
using Site.DesignTokens.Persistence;

namespace Site.DesignTokens.Management;

public sealed class DesignTokenManagementResult
{
    public required bool Success { get; init; }

    public DesignTokenDocument? Document { get; init; }

    public DesignTokenBuildReport? BuildReport { get; init; }

    public IReadOnlyList<DesignTokenDiagnosticViewModel> Tokens { get; init; } = [];

    public IReadOnlyList<DesignTokenDiagnostic> Errors { get; init; } = [];

    public IReadOnlyList<DesignTokenDiagnostic> Warnings { get; init; } = [];
}
