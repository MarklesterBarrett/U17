using Site.DesignTokens.Diagnostics;
using Site.DesignTokens.Persistence;

namespace Site.DesignTokens.Management;

public sealed class DesignTokenStatusSnapshot
{
    public DesignTokenDocument? ActiveDocument { get; init; }

    public DesignTokenDocument? DraftDocument { get; init; }

    public DesignTokenBuildReport? LatestBuildReport { get; init; }

    public DateTime? LatestBuildDateUtc { get; init; }

    public DateTime? LatestSuccessfulBuildDateUtc { get; init; }

    public string? LatestBuildUpdatedBy { get; init; }
}
