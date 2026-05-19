using Site.DesignTokens.Diagnostics;
using Site.DesignTokens.Export;
using Site.DesignTokens.Import;
using Site.DesignTokens.Persistence;
using Site.DesignTokens.Themes;

namespace Site.DesignTokens.Management;

public sealed class DesignTokenManagementService : IDesignTokenManagementService
{
    private readonly object _buildSync = new();
    private readonly IDesignTokenDocumentStore _documentStore;
    private readonly IDesignTokenDiagnosticsService _diagnosticsService;
    private readonly IDesignTokenImportService _importService;
    private readonly IDesignTokenExportService _exportService;
    private readonly IDesignTokenBuildStatusStore _buildStatusStore;
    private readonly IDesignTokenOutputWriter _outputWriter;
    private readonly DesignTokenManagementOptions _options;

    public DesignTokenManagementService(
        IDesignTokenDocumentStore documentStore,
        IDesignTokenDiagnosticsService diagnosticsService,
        IDesignTokenImportService importService,
        IDesignTokenExportService exportService,
        IDesignTokenBuildStatusStore buildStatusStore,
        IDesignTokenOutputWriter outputWriter,
        DesignTokenManagementOptions? options = null)
    {
        _documentStore = documentStore;
        _diagnosticsService = diagnosticsService;
        _importService = importService;
        _exportService = exportService;
        _buildStatusStore = buildStatusStore;
        _outputWriter = outputWriter;
        _options = options ?? new DesignTokenManagementOptions();
    }

    public DesignTokenStatusSnapshot GetStatus()
    {
        var active = _documentStore.GetActive();
        var draft = _documentStore.List()
            .Where(x => x.Status is DesignTokenDocumentStatus.Draft or DesignTokenDocumentStatus.Invalid)
            .OrderByDescending(x => x.UpdatedDateUtc)
            .ThenByDescending(x => x.CreatedDateUtc)
            .FirstOrDefault();
        var buildStatus = _buildStatusStore.Get();

        return new DesignTokenStatusSnapshot
        {
            ActiveDocument = active,
            DraftDocument = draft,
            LatestBuildReport = buildStatus?.LatestReport,
            LatestBuildDateUtc = buildStatus?.UpdatedDateUtc,
            LatestSuccessfulBuildDateUtc = buildStatus?.LatestSuccessfulBuildDateUtc,
            LatestBuildUpdatedBy = buildStatus?.UpdatedBy
        };
    }

    public DesignTokenDocument? GetActiveDocument() => _documentStore.GetActive();

    public string ExportActiveJson() => _exportService.Export(DesignTokenExportMode.ActiveImportedOnly);

    public DesignTokenDiagnosticsResult Validate(string json) => _diagnosticsService.Inspect(json);

    public DesignTokenManagementResult SaveDraft(string json, string? name, string? user)
    {
        var result = _importService.Import(json, name, user, activate: false);
        var document = result.DocumentId is null ? null : _documentStore.GetById(result.DocumentId.Value);
        var diagnostics = _diagnosticsService.Inspect(json);

        return new DesignTokenManagementResult
        {
            Success = result.Success,
            Document = document,
            BuildReport = diagnostics.BuildReport,
            Tokens = diagnostics.Tokens,
            Errors = result.Errors,
            Warnings = diagnostics.BuildReport.Warnings
        };
    }

    public DesignTokenManagementResult Activate(Guid documentId, string? user)
    {
        lock (_buildSync)
        {
            var document = _documentStore.GetById(documentId);
            if (document is null)
            {
                return CreateFailure(
                    null,
                    null,
                    [new DesignTokenDiagnostic(DesignTokenDiagnosticStage.SourceMerge, $"Design token document '{documentId}' was not found.")],
                    []);
            }

            return BuildAndCommit(document, user, activateDocument: true);
        }
    }

    public DesignTokenManagementResult Rebuild(string? user)
    {
        lock (_buildSync)
        {
            var active = _documentStore.GetActive();
            if (active is null)
            {
                return CreateFailure(
                    null,
                    null,
                    [new DesignTokenDiagnostic(DesignTokenDiagnosticStage.SourceMerge, "No active design token document exists.")],
                    []);
            }

            return BuildAndCommit(active, user, activateDocument: false);
        }
    }

    public IReadOnlyList<DesignTokenDiagnosticViewModel> GetTokens()
    {
        var active = _documentStore.GetActive();
        return active is null ? [] : _diagnosticsService.GetTokens(active.Json);
    }

    public DesignTokenDiagnosticViewModel? GetToken(string path)
    {
        var active = _documentStore.GetActive();
        return active is null ? null : _diagnosticsService.GetToken(path, active.Json);
    }

    public DesignTokenPreviewResult PreviewBuild(string json)
    {
        var diagnostics = _diagnosticsService.Inspect(json);
        return CreatePreviewResult(diagnostics, includeCss: diagnostics.BuildReport.ErrorCount == 0);
    }

    public DesignTokenPreviewResult PreviewValidate(string json)
    {
        var diagnostics = _diagnosticsService.Inspect(json);
        return CreatePreviewResult(diagnostics, includeCss: false);
    }

    public DesignTokenPickerResult GetPickerItems(string? query = null, string? tokenType = null, string? sourceType = null, string? context = null, int limit = 100)
    {
        var active = _documentStore.GetActive();
        if (active is null)
        {
            return new DesignTokenPickerResult
            {
                HasActiveBuild = false,
                EmptyMessage = "No active design token build is available. Validate and activate a token document first.",
                AppliedContext = context,
                AppliedTokenType = DesignTokenPickerContext.ResolveTokenType(context) ?? Normalize(tokenType)
            };
        }

        var appliedTokenType = DesignTokenPickerContext.ResolveTokenType(context) ?? Normalize(tokenType);
        var normalizedSourceType = Normalize(sourceType);
        var normalizedQuery = Normalize(query);

        var items = _diagnosticsService.GetTokens(active.Json)
            .Select(CreatePickerItem)
            .Where(x => MatchesContext(x, appliedTokenType))
            .Where(x => MatchesSourceType(x, normalizedSourceType))
            .Where(x => MatchesQuery(x, normalizedQuery))
            .OrderBy(x => x.Path, StringComparer.Ordinal)
            .Take(limit <= 0 ? 100 : limit)
            .ToArray();

        return new DesignTokenPickerResult
        {
            HasActiveBuild = true,
            EmptyMessage = items.Length == 0 ? "No tokens matched the current picker filters." : null,
            AppliedContext = context,
            AppliedTokenType = appliedTokenType,
            Items = items
        };
    }

    public DesignTokenPickerItem? GetPickerItem(string path)
    {
        var active = _documentStore.GetActive();
        if (active is null)
        {
            return null;
        }

        var token = _diagnosticsService.GetToken(path, active.Json);
        return token is null ? null : CreatePickerItem(token);
    }

    private DesignTokenManagementResult BuildAndCommit(DesignTokenDocument document, string? user, bool activateDocument)
    {
        var diagnostics = _diagnosticsService.Inspect(document.Json);
        if (diagnostics.BuildReport.ErrorCount > 0)
        {
            SaveStatus(document, user, diagnostics.BuildReport, success: false);
            return CreateFailure(document, diagnostics.BuildReport, diagnostics.BuildReport.Errors, diagnostics.BuildReport.Warnings);
        }

        if (_options.FailOnWarnings && diagnostics.BuildReport.WarningCount > 0)
        {
            SaveStatus(document, user, diagnostics.BuildReport, success: false);
            return CreateFailure(document, diagnostics.BuildReport, [], diagnostics.BuildReport.Warnings);
        }

        var outputErrors = _outputWriter.Write(diagnostics.Css, diagnostics.TailwindJson);
        if (outputErrors.Count > 0)
        {
            var failedReport = new DesignTokenBuildReport
            {
                Success = false,
                GeneratedCssPath = diagnostics.BuildReport.GeneratedCssPath,
                GeneratedTailwindPath = diagnostics.BuildReport.GeneratedTailwindPath,
                TokenCount = diagnostics.BuildReport.TokenCount,
                InfoCount = diagnostics.BuildReport.InfoCount,
                WarningCount = diagnostics.BuildReport.WarningCount,
                ErrorCount = diagnostics.BuildReport.ErrorCount + outputErrors.Count,
                PipelineStages = diagnostics.BuildReport.PipelineStages,
                Infos = diagnostics.BuildReport.Infos,
                Warnings = diagnostics.BuildReport.Warnings,
                Errors = diagnostics.BuildReport.Errors.Concat(outputErrors).ToArray()
            };

            SaveStatus(document, user, failedReport, success: false);
            return CreateFailure(document, failedReport, failedReport.Errors, failedReport.Warnings);
        }

        var savedDocument = activateDocument
            ? _documentStore.Activate(document.Id, user)
            : document;

        SaveStatus(savedDocument, user, diagnostics.BuildReport, success: true);

        return new DesignTokenManagementResult
        {
            Success = true,
            Document = savedDocument,
            BuildReport = diagnostics.BuildReport,
            Tokens = diagnostics.Tokens,
            Errors = [],
            Warnings = diagnostics.BuildReport.Warnings
        };
    }

    private void SaveStatus(DesignTokenDocument? document, string? user, DesignTokenBuildReport? report, bool success)
    {
        var now = DateTime.UtcNow;
        var existing = _buildStatusStore.Get();
        _buildStatusStore.Save(new DesignTokenBuildStatusRecord
        {
            UpdatedDateUtc = now,
            LatestSuccessfulBuildDateUtc = success
                ? now
                : existing?.LatestSuccessfulBuildDateUtc,
            DocumentId = document?.Id,
            DocumentName = document?.Name,
            UpdatedBy = user ?? document?.UpdatedBy,
            LatestReport = report
        });
    }

    private static DesignTokenManagementResult CreateFailure(
        DesignTokenDocument? document,
        DesignTokenBuildReport? report,
        IReadOnlyList<DesignTokenDiagnostic> errors,
        IReadOnlyList<DesignTokenDiagnostic> warnings)
    {
        return new DesignTokenManagementResult
        {
            Success = false,
            Document = document,
            BuildReport = report,
            Tokens = [],
            Errors = errors,
            Warnings = warnings
        };
    }

    private static DesignTokenPickerItem CreatePickerItem(DesignTokenDiagnosticViewModel token) => new()
    {
        Path = token.Path,
        Type = Normalize(token.Type),
        Label = token.Path,
        SourceType = Normalize(token.SourceType.ToString()),
        SourceName = token.SourceName,
        ResolvedValuePreview = token.ResolvedValue ?? token.NormalizedValue ?? token.RawValue,
        CssVariableName = token.GeneratedCssVariableName,
        ReferenceValue = $"{{{token.Path}}}"
    };

    private static bool MatchesContext(DesignTokenPickerItem item, string? appliedTokenType) =>
        string.IsNullOrWhiteSpace(appliedTokenType) ||
        string.Equals(item.Type, appliedTokenType, StringComparison.Ordinal);

    private static bool MatchesSourceType(DesignTokenPickerItem item, string normalizedSourceType) =>
        string.IsNullOrWhiteSpace(normalizedSourceType) ||
        string.Equals(item.SourceType, normalizedSourceType, StringComparison.Ordinal);

    private static bool MatchesQuery(DesignTokenPickerItem item, string normalizedQuery)
    {
        if (string.IsNullOrWhiteSpace(normalizedQuery))
        {
            return true;
        }

        return Normalize(item.Path).Contains(normalizedQuery, StringComparison.Ordinal) ||
               Normalize(item.Type).Contains(normalizedQuery, StringComparison.Ordinal) ||
               Normalize(item.SourceName).Contains(normalizedQuery, StringComparison.Ordinal);
    }

    private static string Normalize(string? value) => DesignTokenPickerContext.Normalize(value);

    private DesignTokenPreviewResult CreatePreviewResult(DesignTokenDiagnosticsResult diagnostics, bool includeCss)
    {
        return new DesignTokenPreviewResult
        {
            Success = diagnostics.BuildReport.ErrorCount == 0,
            PreviewCss = includeCss ? diagnostics.Css : string.Empty,
            TailwindJson = diagnostics.TailwindJson,
            TokenCount = diagnostics.BuildReport.TokenCount,
            Tokens = diagnostics.Tokens,
            Errors = diagnostics.BuildReport.Errors,
            Warnings = diagnostics.BuildReport.Warnings,
            BuildReport = diagnostics.BuildReport,
            Comparison = BuildComparison(diagnostics),
            ThemeVariants = diagnostics.ThemeVariants,
            SourceSummaries = diagnostics.SourceSummaries,
            SourceSummaryTotals = diagnostics.SourceSummaryTotals,
            MergeEvents = diagnostics.MergeEvents,
            DiagnosticsExport = diagnostics.DiagnosticsExport
        };
    }

    private DesignTokenPreviewComparisonSummary BuildComparison(DesignTokenDiagnosticsResult draftDiagnostics)
    {
        var activeDocument = _documentStore.GetActive();
        var activeDiagnostics = activeDocument is null
            ? null
            : _diagnosticsService.Inspect(activeDocument.Json);

        var activeMap = (activeDiagnostics?.Tokens ?? [])
            .ToDictionary(x => x.Path, x => x.ResolvedValue ?? x.NormalizedValue ?? x.RawValue, StringComparer.Ordinal);
        var draftMap = draftDiagnostics.Tokens
            .ToDictionary(x => x.Path, x => x.ResolvedValue ?? x.NormalizedValue ?? x.RawValue, StringComparer.Ordinal);

        var changed = new List<DesignTokenPreviewComparisonItem>();
        var added = new List<DesignTokenPreviewComparisonItem>();
        var removed = new List<DesignTokenPreviewComparisonItem>();

        foreach (var draft in draftMap.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            if (!activeMap.TryGetValue(draft.Key, out var activeValue))
            {
                added.Add(new DesignTokenPreviewComparisonItem
                {
                    Path = draft.Key,
                    ActiveValue = null,
                    DraftValue = draft.Value,
                    ChangeType = "Added"
                });
                continue;
            }

            if (!string.Equals(activeValue, draft.Value, StringComparison.Ordinal))
            {
                changed.Add(new DesignTokenPreviewComparisonItem
                {
                    Path = draft.Key,
                    ActiveValue = activeValue,
                    DraftValue = draft.Value,
                    ChangeType = "Changed"
                });
            }
        }

        foreach (var active in activeMap.OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            if (draftMap.ContainsKey(active.Key))
            {
                continue;
            }

            removed.Add(new DesignTokenPreviewComparisonItem
            {
                Path = active.Key,
                ActiveValue = active.Value,
                DraftValue = null,
                ChangeType = "Removed"
            });
        }

        return new DesignTokenPreviewComparisonSummary
        {
            ChangedTokenCount = changed.Count,
            AddedTokenCount = added.Count,
            RemovedTokenCount = removed.Count,
            ChangedTokens = changed,
            AddedTokens = added,
            RemovedTokens = removed
        };
    }
}
