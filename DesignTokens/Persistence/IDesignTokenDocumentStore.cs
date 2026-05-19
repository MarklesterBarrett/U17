namespace Site.DesignTokens.Persistence;

public interface IDesignTokenDocumentStore
{
    DesignTokenDocument? GetActive();

    DesignTokenDocument? GetById(Guid id);

    DesignTokenDocument SaveDraft(DesignTokenDocument document);

    DesignTokenDocument Activate(Guid id, string? updatedBy = null);

    DesignTokenDocument Archive(Guid id, string? updatedBy = null);

    IReadOnlyList<DesignTokenDocument> List();
}
