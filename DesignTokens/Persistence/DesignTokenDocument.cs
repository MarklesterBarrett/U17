namespace Site.DesignTokens.Persistence;

public sealed record class DesignTokenDocument
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Json { get; init; } = string.Empty;

    public DesignTokenDocumentStatus Status { get; init; }

    public DateTime CreatedDateUtc { get; init; }

    public DateTime UpdatedDateUtc { get; init; }

    public string? CreatedBy { get; init; }

    public string? UpdatedBy { get; init; }

    public string? Hash { get; init; }

    public int Version { get; init; }

    public string? ValidationSummary { get; init; }
}
