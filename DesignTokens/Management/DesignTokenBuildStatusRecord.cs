using Site.DesignTokens.Diagnostics;

namespace Site.DesignTokens.Management;

public sealed class DesignTokenBuildStatusRecord
{
    public DateTime UpdatedDateUtc { get; init; }

    public DateTime? LatestSuccessfulBuildDateUtc { get; init; }

    public Guid? DocumentId { get; init; }

    public string? DocumentName { get; init; }

    public string? UpdatedBy { get; init; }

    public DesignTokenBuildReport? LatestReport { get; init; }
}
