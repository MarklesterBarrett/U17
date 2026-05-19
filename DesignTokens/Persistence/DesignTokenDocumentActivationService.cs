using Site.DesignTokens.Css;
using Site.DesignTokens.Diagnostics;
using Site.DesignTokens.Import;
using Site.DesignTokens;

namespace Site.DesignTokens.Persistence;

public sealed class DesignTokenDocumentActivationService : IDesignTokenDocumentActivationService
{
    private readonly IDesignTokenDocumentStore _documentStore;
    private readonly DesignTokenBuildPipeline _buildPipeline;
    private readonly IDesignTokenCssWriter _cssWriter;

    public DesignTokenDocumentActivationService(
        IDesignTokenDocumentStore documentStore,
        DesignTokenBuildPipeline buildPipeline,
        IDesignTokenCssWriter cssWriter)
    {
        _documentStore = documentStore;
        _buildPipeline = buildPipeline;
        _cssWriter = cssWriter;
    }

    public DesignTokenDocumentActivationResult Activate(Guid id)
    {
        var document = _documentStore.GetById(id);
        if (document is null)
        {
            return new DesignTokenDocumentActivationResult(
                false,
                null,
                [new DesignTokenDiagnostic(DesignTokenDiagnosticStage.SourceMerge, $"Design token document '{id}' was not found.")]);
        }

        var buildResult = _buildPipeline.Build(document.Json);
        if (!buildResult.Success)
        {
            return new DesignTokenDocumentActivationResult(false, id, DesignTokenImportService.CreateDiagnostics(buildResult));
        }

        var previousActive = _documentStore.GetActive();

        try
        {
            var activated = _documentStore.Activate(id);

            try
            {
                _cssWriter.Write(buildResult.Css);
            }
            catch (Exception exception)
            {
                RestorePreviousActiveDocument(previousActive, activated.Id);

                return new DesignTokenDocumentActivationResult(
                    false,
                    id,
                    [new DesignTokenDiagnostic(DesignTokenDiagnosticStage.CssWrite, exception.Message)]);
            }

            return new DesignTokenDocumentActivationResult(true, activated.Id, []);
        }
        catch (Exception exception)
        {
            return new DesignTokenDocumentActivationResult(
                false,
                id,
                [new DesignTokenDiagnostic(DesignTokenDiagnosticStage.SourceMerge, exception.Message)]);
        }
    }

    private void RestorePreviousActiveDocument(DesignTokenDocument? previousActive, Guid attemptedId)
    {
        if (previousActive is not null && previousActive.Id != attemptedId)
        {
            _documentStore.Activate(previousActive.Id);
            return;
        }

        _documentStore.Archive(attemptedId);
    }
}
