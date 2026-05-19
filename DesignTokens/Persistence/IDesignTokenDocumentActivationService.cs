namespace Site.DesignTokens.Persistence;

public interface IDesignTokenDocumentActivationService
{
    DesignTokenDocumentActivationResult Activate(Guid id);
}
