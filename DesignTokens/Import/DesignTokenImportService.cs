using System.Security.Cryptography;
using System.Text;
using Site.DesignTokens;
using Site.DesignTokens.Diagnostics;
using Site.DesignTokens.Persistence;

namespace Site.DesignTokens.Import;

public sealed class DesignTokenImportService : IDesignTokenImportService
{
    private readonly DesignTokenBuildPipeline _buildPipeline;
    private readonly IDesignTokenDocumentStore _documentStore;
    private readonly IDesignTokenDocumentActivationService _activationService;

    public DesignTokenImportService(
        DesignTokenBuildPipeline buildPipeline,
        IDesignTokenDocumentStore documentStore,
        IDesignTokenDocumentActivationService activationService)
    {
        _buildPipeline = buildPipeline;
        _documentStore = documentStore;
        _activationService = activationService;
    }

    public DesignTokenImportResult Import(string json, string? name = null, string? user = null, bool activate = false)
    {
        var buildResult = _buildPipeline.Build(json);
        var diagnostics = CreateDiagnostics(buildResult);
        var parseCount = buildResult.Registry.All.Count;

        if (!buildResult.Success)
        {
            var invalidDocument = _documentStore.SaveDraft(CreateDocument(
                json,
                name,
                user,
                DesignTokenDocumentStatus.Invalid,
                diagnostics));

            return new DesignTokenImportResult(
                false,
                invalidDocument.Id,
                diagnostics,
                [],
                parseCount,
                null);
        }

        var draftDocument = _documentStore.SaveDraft(CreateDocument(
            json,
            name,
            user,
            DesignTokenDocumentStatus.Draft,
            []));

        if (activate)
        {
            var activationResult = _activationService.Activate(draftDocument.Id);
            if (!activationResult.Success)
            {
                return new DesignTokenImportResult(
                    false,
                    draftDocument.Id,
                    activationResult.Errors,
                    [],
                    parseCount,
                    buildResult.Css);
            }
        }

        return new DesignTokenImportResult(
            true,
            draftDocument.Id,
            [],
            [],
            parseCount,
            buildResult.Css);
    }

    private static DesignTokenDocument CreateDocument(
        string json,
        string? name,
        string? user,
        DesignTokenDocumentStatus status,
        IReadOnlyList<DesignTokenDiagnostic> diagnostics)
    {
        var now = DateTime.UtcNow;
        return new DesignTokenDocument
        {
            Name = string.IsNullOrWhiteSpace(name) ? $"Imported tokens {now:yyyy-MM-dd HH:mm:ss} UTC" : name,
            Json = json,
            Status = status,
            CreatedDateUtc = now,
            UpdatedDateUtc = now,
            CreatedBy = user,
            UpdatedBy = user,
            Hash = ComputeHash(json),
            ValidationSummary = diagnostics.Count == 0
                ? null
                : string.Join(Environment.NewLine, diagnostics.Select(x => $"{x.Stage}: {x.Message}"))
        };
    }

    internal static IReadOnlyList<DesignTokenDiagnostic> CreateDiagnostics(DesignTokenBuildPipelineResult buildResult)
    {
        var diagnostics = new List<DesignTokenDiagnostic>();
        diagnostics.AddRange(buildResult.SourceMergeErrors.Select(x => new DesignTokenDiagnostic(DesignTokenDiagnosticStage.SourceMerge, x.Message, x.Path)));
        diagnostics.AddRange(buildResult.ParseErrors.Select(x => new DesignTokenDiagnostic(DesignTokenDiagnosticStage.Parse, x.Message, x.Path)));
        diagnostics.AddRange(buildResult.NormalisationErrors.Select(x => new DesignTokenDiagnostic(DesignTokenDiagnosticStage.Normalise, x.Message, x.Path)));
        diagnostics.AddRange(buildResult.ResolutionErrors.Select(x => new DesignTokenDiagnostic(DesignTokenDiagnosticStage.Resolve, x.Message, x.SourcePath)));
        diagnostics.AddRange(buildResult.ValidationErrors.Select(x => new DesignTokenDiagnostic(DesignTokenDiagnosticStage.Validate, x.Message, x.Path, x.Field)));
        diagnostics.AddRange(buildResult.CssGenerationErrors.Select(x => new DesignTokenDiagnostic(DesignTokenDiagnosticStage.CssGenerate, x.Message, x.Path)));
        return diagnostics;
    }

    private static string ComputeHash(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty));
        return Convert.ToHexString(bytes);
    }
}
