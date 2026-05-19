using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Site.DesignTokens.Diagnostics;
using Site.DesignTokens.Export;
using Site.DesignTokens.Persistence;
using Site.DesignTokens.Themes;
using Umbraco.Cms.Api.Management.Controllers;
using Umbraco.Cms.Api.Management.Routing;

namespace Site.DesignTokens.Management;

[Authorize]
[VersionedApiBackOfficeRoute("design-tokens")]
public sealed class DesignTokenManagementController : ManagementApiControllerBase
{
    private readonly IDesignTokenManagementService _managementService;
    private readonly IFoundationTokensService _foundationTokensService;
    private readonly IDesignTokenBackofficeAccessService _accessService;
    private readonly IDesignTokenExportService _exportService;
    private readonly DesignTokenManagementOptions _options;

    public DesignTokenManagementController(
        IDesignTokenManagementService managementService,
        IFoundationTokensService foundationTokensService,
        IDesignTokenBackofficeAccessService accessService,
        IDesignTokenExportService exportService,
        DesignTokenManagementOptions? options = null)
    {
        _managementService = managementService;
        _foundationTokensService = foundationTokensService;
        _accessService = accessService;
        _exportService = exportService;
        _options = options ?? new DesignTokenManagementOptions();
    }

    [HttpGet("status")]
    public ActionResult<DesignTokenStatusResponse> GetStatus()
    {
        var denial = DenyWhenUnauthorized();
        if (denial is not null)
        {
            return denial;
        }

        var status = _managementService.GetStatus();
        return Ok(DesignTokenStatusResponse.From(status));
    }

    [HttpGet("active")]
    public ActionResult<DesignTokenActiveDocumentResponse> GetActive()
    {
        var denial = DenyWhenUnauthorized();
        if (denial is not null)
        {
            return denial;
        }

        var document = _managementService.GetActiveDocument();
        if (document is null)
        {
            return NotFound();
        }

        var status = _managementService.GetStatus();
        return Ok(DesignTokenActiveDocumentResponse.From(document, status));
    }

    [HttpGet("starter")]
    public ActionResult ExportStarter()
    {
        var denial = DenyWhenUnauthorized();
        if (denial is not null)
        {
            return denial;
        }

        return Content(_exportService.Export(DesignTokenExportMode.Starter), "text/plain");
    }

    [HttpGet("tokens")]
    public ActionResult<IReadOnlyList<DesignTokenTokenListItemResponse>> GetTokens()
    {
        var denial = DenyWhenUnauthorized();
        if (denial is not null)
        {
            return denial;
        }

        return Ok(_managementService.GetTokens().Select(DesignTokenTokenListItemResponse.From).ToArray());
    }

    [HttpGet("foundation")]
    public ActionResult<FoundationTokensStateResponse> GetFoundationTokens()
    {
        var denial = DenyWhenUnauthorized();
        if (denial is not null)
        {
            return denial;
        }

        return Ok(_foundationTokensService.GetState());
    }

    [HttpGet("tokens/{*path}")]
    public ActionResult<DesignTokenTokenDetailResponse> GetToken(string path)
    {
        var denial = DenyWhenUnauthorized();
        if (denial is not null)
        {
            return denial;
        }

        var token = _managementService.GetToken(path);
        return token is null ? NotFound() : Ok(DesignTokenTokenDetailResponse.From(token));
    }

    [HttpGet("picker")]
    public ActionResult<DesignTokenPickerResponse> GetPicker(
        [FromQuery] string? query = null,
        [FromQuery] string? tokenType = null,
        [FromQuery] string? sourceType = null,
        [FromQuery] string? context = null,
        [FromQuery] int limit = 100)
    {
        var denial = DenyWhenUnauthorized();
        if (denial is not null)
        {
            return denial;
        }

        return Ok(DesignTokenPickerResponse.From(_managementService.GetPickerItems(query, tokenType, sourceType, context, limit)));
    }

    [HttpGet("picker/search")]
    public ActionResult<DesignTokenPickerResponse> SearchPicker(
        [FromQuery] string? query = null,
        [FromQuery] string? tokenType = null,
        [FromQuery] string? sourceType = null,
        [FromQuery] string? context = null,
        [FromQuery] int limit = 100)
    {
        var denial = DenyWhenUnauthorized();
        if (denial is not null)
        {
            return denial;
        }

        return Ok(DesignTokenPickerResponse.From(_managementService.GetPickerItems(query, tokenType, sourceType, context, limit)));
    }

    [HttpGet("picker/{*path}")]
    public ActionResult<DesignTokenPickerItemResponse> GetPickerItem(string path, [FromQuery] string? context = null)
    {
        var denial = DenyWhenUnauthorized();
        if (denial is not null)
        {
            return denial;
        }

        var item = _managementService.GetPickerItem(path);
        if (item is null)
        {
            return NotFound();
        }

        var appliedType = DesignTokenPickerContext.ResolveTokenType(context);
        if (!string.IsNullOrWhiteSpace(appliedType) && !string.Equals(item.Type, appliedType, StringComparison.Ordinal))
        {
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["context"] = ["Selected token is not compatible with the requested picker context."]
            }));
        }

        return Ok(DesignTokenPickerItemResponse.From(item));
    }

    [HttpPost("validate")]
    public ActionResult<DesignTokenOperationResponse> Validate([FromBody] DesignTokenValidateRequest request)
    {
        var denial = DenyWhenUnauthorized();
        if (denial is not null)
        {
            return denial;
        }

        var diagnostics = _managementService.Validate(request.Json ?? string.Empty);
        return Ok(DesignTokenOperationResponse.FromDiagnostics(diagnostics, null));
    }

    [HttpPost("save-draft")]
    public ActionResult<DesignTokenOperationResponse> SaveDraft([FromBody] DesignTokenSaveDraftRequest request)
    {
        var denial = DenyWhenUnauthorized();
        if (denial is not null)
        {
            return denial;
        }

        var result = _managementService.SaveDraft(request.Json ?? string.Empty, request.Name, User?.Identity?.Name);
        return Ok(DesignTokenOperationResponse.FromManagementResult(result));
    }

    [HttpPost("activate")]
    public ActionResult<DesignTokenOperationResponse> Activate([FromBody] DesignTokenActivateRequest request)
    {
        var denial = DenyWhenUnauthorized();
        if (denial is not null)
        {
            return denial;
        }

        var result = _managementService.Activate(request.DocumentId, User?.Identity?.Name);
        return Ok(DesignTokenOperationResponse.FromManagementResult(result));
    }

    [HttpPost("rebuild")]
    public ActionResult<DesignTokenOperationResponse> Rebuild()
    {
        var denial = DenyWhenUnauthorized();
        if (denial is not null)
        {
            return denial;
        }

        var result = _managementService.Rebuild(User?.Identity?.Name);
        return Ok(DesignTokenOperationResponse.FromManagementResult(result));
    }

    [HttpPost("preview/validate")]
    [RequestSizeLimit(500_000)]
    public ActionResult<DesignTokenPreviewResponse> PreviewValidate([FromBody] DesignTokenPreviewRequest request)
    {
        var denial = DenyWhenUnauthorized();
        if (denial is not null)
        {
            return denial;
        }

        var sizeProblem = ValidatePreviewRequestSize(request.Json);
        if (sizeProblem is not null)
        {
            return sizeProblem;
        }

        return Ok(DesignTokenPreviewResponse.From(_managementService.PreviewValidate(request.Json ?? string.Empty)));
    }

    [HttpPost("preview/build")]
    [RequestSizeLimit(500_000)]
    public ActionResult<DesignTokenPreviewResponse> PreviewBuild([FromBody] DesignTokenPreviewRequest request)
    {
        var denial = DenyWhenUnauthorized();
        if (denial is not null)
        {
            return denial;
        }

        var sizeProblem = ValidatePreviewRequestSize(request.Json);
        if (sizeProblem is not null)
        {
            return sizeProblem;
        }

        return Ok(DesignTokenPreviewResponse.From(_managementService.PreviewBuild(request.Json ?? string.Empty)));
    }

    [HttpPost("foundation/validate")]
    [RequestSizeLimit(500_000)]
    public ActionResult<DesignTokenOperationResponse> ValidateFoundation([FromBody] FoundationTokensRequest request)
    {
        var denial = DenyWhenUnauthorized();
        if (denial is not null)
        {
            return denial;
        }

        var diagnostics = _foundationTokensService.Validate(request);
        return Ok(DesignTokenOperationResponse.FromDiagnostics(diagnostics, null));
    }

    [HttpPost("foundation/preview")]
    [RequestSizeLimit(500_000)]
    public ActionResult<DesignTokenPreviewResponse> PreviewFoundation([FromBody] FoundationTokensRequest request)
    {
        var denial = DenyWhenUnauthorized();
        if (denial is not null)
        {
            return denial;
        }

        return Ok(DesignTokenPreviewResponse.From(_foundationTokensService.Preview(request)));
    }

    [HttpPost("foundation/save-draft")]
    [RequestSizeLimit(500_000)]
    public ActionResult<DesignTokenOperationResponse> SaveFoundationDraft([FromBody] FoundationTokensRequest request)
    {
        var denial = DenyWhenUnauthorized();
        if (denial is not null)
        {
            return denial;
        }

        var result = _foundationTokensService.SaveDraft(request, User?.Identity?.Name);
        return Ok(DesignTokenOperationResponse.FromManagementResult(result));
    }

    [HttpPost("foundation/publish")]
    [RequestSizeLimit(500_000)]
    public ActionResult<DesignTokenOperationResponse> PublishFoundation([FromBody] FoundationTokensRequest request)
    {
        var denial = DenyWhenUnauthorized();
        if (denial is not null)
        {
            return denial;
        }

        var result = _foundationTokensService.Publish(request, User?.Identity?.Name);
        return Ok(DesignTokenOperationResponse.FromManagementResult(result));
    }

    [HttpGet("export")]
    public ActionResult Export()
    {
        var denial = DenyWhenUnauthorized();
        if (denial is not null)
        {
            return denial;
        }

        return Content(_managementService.ExportActiveJson(), "application/json");
    }

    private ActionResult? DenyWhenUnauthorized() =>
        _accessService.HasAccess(User)
            ? null
            : Forbid();

    private ActionResult<DesignTokenPreviewResponse>? ValidatePreviewRequestSize(string? json)
    {
        if ((json?.Length ?? 0) <= _options.MaxPreviewJsonLength)
        {
            return null;
        }

        return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
        {
            ["json"] = [$"Preview JSON exceeds maximum allowed length of {_options.MaxPreviewJsonLength} characters."]
        }));
    }
}

public sealed record DesignTokenValidateRequest(string? Json);

public sealed record DesignTokenSaveDraftRequest(string? Name, string? Json);

public sealed record DesignTokenActivateRequest(Guid DocumentId);

public sealed record DesignTokenPreviewRequest(string? Json);

public sealed class DesignTokenStatusResponse
{
    public DesignTokenActiveDocumentResponse? ActiveDocument { get; init; }

    public DesignTokenDraftDocumentResponse? DraftDocument { get; init; }

    public DesignTokenBuildReportResponse? LatestBuild { get; init; }

    public DateTime? LatestBuildDateUtc { get; init; }

    public DateTime? LatestSuccessfulBuildDateUtc { get; init; }

    public string? LatestBuildUpdatedBy { get; init; }

    public static DesignTokenStatusResponse From(DesignTokenStatusSnapshot snapshot) => new()
    {
        ActiveDocument = snapshot.ActiveDocument is null ? null : DesignTokenActiveDocumentResponse.From(snapshot.ActiveDocument, snapshot),
        DraftDocument = snapshot.DraftDocument is null ? null : DesignTokenDraftDocumentResponse.From(snapshot.DraftDocument),
        LatestBuild = snapshot.LatestBuildReport is null ? null : DesignTokenBuildReportResponse.From(snapshot.LatestBuildReport),
        LatestBuildDateUtc = snapshot.LatestBuildDateUtc,
        LatestSuccessfulBuildDateUtc = snapshot.LatestSuccessfulBuildDateUtc,
        LatestBuildUpdatedBy = snapshot.LatestBuildUpdatedBy
    };
}

public sealed class DesignTokenDraftDocumentResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Status { get; init; }

    public required string Json { get; init; }

    public required DateTime CreatedDateUtc { get; init; }

    public required DateTime UpdatedDateUtc { get; init; }

    public string? UpdatedBy { get; init; }

    public static DesignTokenDraftDocumentResponse From(DesignTokenDocument document) => new()
    {
        Id = document.Id,
        Name = document.Name,
        Status = document.Status.ToString(),
        Json = document.Json,
        CreatedDateUtc = document.CreatedDateUtc,
        UpdatedDateUtc = document.UpdatedDateUtc,
        UpdatedBy = document.UpdatedBy
    };
}

public sealed class DesignTokenActiveDocumentResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Status { get; init; }

    public required string Json { get; init; }

    public required DateTime CreatedDateUtc { get; init; }

    public required DateTime UpdatedDateUtc { get; init; }

    public string? UpdatedBy { get; init; }

    public int TokenCount { get; init; }

    public DateTime? LatestSuccessfulBuildDateUtc { get; init; }

    public string? GeneratedCssPath { get; init; }

    public string? GeneratedTailwindPath { get; init; }

    public static DesignTokenActiveDocumentResponse From(DesignTokenDocument document, DesignTokenStatusSnapshot status) => new()
    {
        Id = document.Id,
        Name = document.Name,
        Status = document.Status.ToString(),
        Json = document.Json,
        CreatedDateUtc = document.CreatedDateUtc,
        UpdatedDateUtc = document.UpdatedDateUtc,
        UpdatedBy = document.UpdatedBy,
        TokenCount = status.LatestBuildReport?.TokenCount ?? 0,
        LatestSuccessfulBuildDateUtc = status.LatestSuccessfulBuildDateUtc,
        GeneratedCssPath = status.LatestBuildReport?.GeneratedCssPath,
        GeneratedTailwindPath = status.LatestBuildReport?.GeneratedTailwindPath
    };
}

public sealed class DesignTokenOperationResponse
{
    public required bool Success { get; init; }

    public DesignTokenDocumentSummaryResponse? Document { get; init; }

    public DesignTokenBuildReportResponse? BuildReport { get; init; }

    public string GeneratedCss { get; init; } = string.Empty;

    public string TailwindJson { get; init; } = string.Empty;

    public IReadOnlyList<DesignTokenDiagnosticResponse> Errors { get; init; } = [];

    public IReadOnlyList<DesignTokenDiagnosticResponse> Infos { get; init; } = [];

    public IReadOnlyList<DesignTokenDiagnosticResponse> Warnings { get; init; } = [];

    public IReadOnlyList<DesignTokenTokenListItemResponse> Tokens { get; init; } = [];

    public IReadOnlyList<DesignTokenSourceSummaryResponse> SourceSummaries { get; init; } = [];

    public DesignTokenSourceSummaryTotalsResponse? SourceSummaryTotals { get; init; }

    public IReadOnlyList<DesignTokenMergeEventResponse> MergeEvents { get; init; } = [];

    public object? DiagnosticsExport { get; init; }

    public static DesignTokenOperationResponse FromDiagnostics(DesignTokenDiagnosticsResult result, DesignTokenDocument? document) => new()
    {
        Success = result.BuildReport.ErrorCount == 0,
        Document = document is null ? null : DesignTokenDocumentSummaryResponse.From(document),
        BuildReport = DesignTokenBuildReportResponse.From(result.BuildReport),
        GeneratedCss = result.Css,
        TailwindJson = result.TailwindJson,
        Errors = result.BuildReport.Errors.Select(DesignTokenDiagnosticResponse.From).ToArray(),
        Infos = result.BuildReport.Infos.Select(DesignTokenDiagnosticResponse.From).ToArray(),
        Warnings = result.BuildReport.Warnings.Select(DesignTokenDiagnosticResponse.From).ToArray(),
        Tokens = result.Tokens.Select(DesignTokenTokenListItemResponse.From).ToArray(),
        SourceSummaries = result.SourceSummaries.Select(DesignTokenSourceSummaryResponse.From).ToArray(),
        SourceSummaryTotals = DesignTokenSourceSummaryTotalsResponse.From(result.SourceSummaryTotals),
        MergeEvents = result.MergeEvents.Select(DesignTokenMergeEventResponse.From).ToArray(),
        DiagnosticsExport = result.DiagnosticsExport
    };

    public static DesignTokenOperationResponse FromManagementResult(DesignTokenManagementResult result) => new()
    {
        Success = result.Success,
        Document = result.Document is null ? null : DesignTokenDocumentSummaryResponse.From(result.Document),
        BuildReport = result.BuildReport is null ? null : DesignTokenBuildReportResponse.From(result.BuildReport),
        Errors = result.Errors.Select(DesignTokenDiagnosticResponse.From).ToArray(),
        Infos = result.BuildReport?.Infos.Select(DesignTokenDiagnosticResponse.From).ToArray() ?? [],
        Warnings = result.Warnings.Select(DesignTokenDiagnosticResponse.From).ToArray(),
        Tokens = result.Tokens.Select(DesignTokenTokenListItemResponse.From).ToArray()
    };
}

public sealed class DesignTokenDocumentSummaryResponse
{
    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string Status { get; init; }

    public required DateTime UpdatedDateUtc { get; init; }

    public string? UpdatedBy { get; init; }

    public static DesignTokenDocumentSummaryResponse From(DesignTokenDocument document) => new()
    {
        Id = document.Id,
        Name = document.Name,
        Status = document.Status.ToString(),
        UpdatedDateUtc = document.UpdatedDateUtc,
        UpdatedBy = document.UpdatedBy
    };
}

public sealed class DesignTokenBuildReportResponse
{
    public required bool Success { get; init; }

    public string? GeneratedCssPath { get; init; }

    public string? GeneratedTailwindPath { get; init; }

    public required int TokenCount { get; init; }

    public required int WarningCount { get; init; }

    public required int ErrorCount { get; init; }

    public int InfoCount { get; init; }

    public IReadOnlyList<DesignTokenPipelineStageResponse> PipelineStages { get; init; } = [];

    public static DesignTokenBuildReportResponse From(DesignTokenBuildReport report) => new()
    {
        Success = report.Success,
        GeneratedCssPath = report.GeneratedCssPath,
        GeneratedTailwindPath = report.GeneratedTailwindPath,
        TokenCount = report.TokenCount,
        WarningCount = report.WarningCount,
        ErrorCount = report.ErrorCount,
        InfoCount = report.InfoCount,
        PipelineStages = report.PipelineStages.Select(DesignTokenPipelineStageResponse.From).ToArray()
    };
}

public sealed class DesignTokenPipelineStageResponse
{
    public required string Stage { get; init; }

    public required bool Success { get; init; }

    public required long DurationMs { get; init; }

    public IReadOnlyList<DesignTokenDiagnosticResponse> Errors { get; init; } = [];

    public IReadOnlyList<DesignTokenDiagnosticResponse> Warnings { get; init; } = [];

    public static DesignTokenPipelineStageResponse From(DesignTokenPipelineStageResult result) => new()
    {
        Stage = result.Stage.ToString(),
        Success = result.Success,
        DurationMs = result.DurationMs,
        Errors = result.Errors.Select(DesignTokenDiagnosticResponse.From).ToArray(),
        Warnings = result.Warnings.Select(DesignTokenDiagnosticResponse.From).ToArray()
    };
}

public sealed class DesignTokenDiagnosticResponse
{
    public required string Stage { get; init; }

    public string? TokenPath { get; init; }

    public string? Field { get; init; }

    public required string Message { get; init; }

    public static DesignTokenDiagnosticResponse From(DesignTokenDiagnostic diagnostic) => new()
    {
        Stage = diagnostic.Stage.ToString(),
        TokenPath = diagnostic.TokenPath,
        Field = diagnostic.Field,
        Message = diagnostic.Message
    };
}

public sealed class DesignTokenTokenListItemResponse
{
    public required string Path { get; init; }

    public required string Type { get; init; }

    public string? Source { get; init; }

    public string? SourceType { get; init; }

    public int SourcePriority { get; init; }

    public string? GeneratedCssVariable { get; init; }

    public string? RawValue { get; init; }

    public string? ResolvedValue { get; init; }

    public string? ValuePreview { get; init; }

    public int OverriddenSourceCount { get; init; }

    public bool IsAdded { get; init; }

    public bool IsOverridden { get; init; }

    public bool IsUnused { get; init; }

    public bool HasGeneratedCss { get; init; }

    public bool IsTailwindMapped { get; init; }

    public bool IsTailwindSkipped { get; init; }

    public static DesignTokenTokenListItemResponse From(DesignTokenDiagnosticViewModel token) => new()
    {
        Path = token.Path,
        Type = token.Type,
        Source = token.SourceName,
        SourceType = token.SourceType.ToString(),
        SourcePriority = token.SourcePriority,
        GeneratedCssVariable = token.GeneratedCssVariableName,
        RawValue = token.RawValue,
        ResolvedValue = token.ResolvedValue,
        ValuePreview = token.ResolvedValue ?? token.NormalizedValue ?? token.RawValue,
        OverriddenSourceCount = token.OverriddenSources.Count,
        IsAdded = token.IsAdded,
        IsOverridden = token.IsOverridden,
        IsUnused = token.IsUnused,
        HasGeneratedCss = token.HasGeneratedCss,
        IsTailwindMapped = token.IsTailwindMapped,
        IsTailwindSkipped = token.IsTailwindSkipped
    };
}

public sealed class DesignTokenTokenDetailResponse
{
    public required string Path { get; init; }

    public required string Type { get; init; }

    public required string SourceType { get; init; }

    public string? SourceName { get; init; }

    public int SourcePriority { get; init; }

    public string? RawValue { get; init; }

    public string? NormalizedValue { get; init; }

    public string? ResolvedValue { get; init; }

    public string? GeneratedCssVariable { get; init; }

    public string? GeneratedCssValue { get; init; }

    public IReadOnlyList<string> OutgoingReferences { get; init; } = [];

    public IReadOnlyList<string> IncomingReferences { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public IReadOnlyList<string> Errors { get; init; } = [];

    public IReadOnlyList<string> Infos { get; init; } = [];

    public IReadOnlyList<DesignTokenSourceDescriptorResponse> OverriddenSources { get; init; } = [];

    public IReadOnlyList<string> ResolutionTrace { get; init; } = [];

    public bool IsAdded { get; init; }

    public bool IsOverridden { get; init; }

    public bool IsUnused { get; init; }

    public bool HasGeneratedCss { get; init; }

    public bool IsTailwindMapped { get; init; }

    public bool IsTailwindSkipped { get; init; }

    public static DesignTokenTokenDetailResponse From(DesignTokenDiagnosticViewModel token) => new()
    {
        Path = token.Path,
        Type = token.Type,
        SourceType = token.SourceType.ToString(),
        SourceName = token.SourceName,
        SourcePriority = token.SourcePriority,
        RawValue = token.RawValue,
        NormalizedValue = token.NormalizedValue,
        ResolvedValue = token.ResolvedValue,
        GeneratedCssVariable = token.GeneratedCssVariableName,
        GeneratedCssValue = token.GeneratedCssValue,
        OutgoingReferences = token.References,
        IncomingReferences = token.ReferencedBy,
        Warnings = token.Warnings,
        Errors = token.Errors,
        Infos = token.Infos,
        OverriddenSources = token.OverriddenSources.Select(DesignTokenSourceDescriptorResponse.From).ToArray(),
        ResolutionTrace = token.ResolutionTrace,
        IsAdded = token.IsAdded,
        IsOverridden = token.IsOverridden,
        IsUnused = token.IsUnused,
        HasGeneratedCss = token.HasGeneratedCss,
        IsTailwindMapped = token.IsTailwindMapped,
        IsTailwindSkipped = token.IsTailwindSkipped
    };
}

public sealed class DesignTokenPickerResponse
{
    public required bool HasActiveBuild { get; init; }

    public string? EmptyMessage { get; init; }

    public string? AppliedContext { get; init; }

    public string? AppliedTokenType { get; init; }

    public IReadOnlyList<DesignTokenPickerItemResponse> Items { get; init; } = [];

    public static DesignTokenPickerResponse From(DesignTokenPickerResult result) => new()
    {
        HasActiveBuild = result.HasActiveBuild,
        EmptyMessage = result.EmptyMessage,
        AppliedContext = result.AppliedContext,
        AppliedTokenType = result.AppliedTokenType,
        Items = result.Items.Select(DesignTokenPickerItemResponse.From).ToArray()
    };
}

public sealed class DesignTokenPickerItemResponse
{
    public required string Path { get; init; }

    public required string Type { get; init; }

    public required string Label { get; init; }

    public required string SourceType { get; init; }

    public string? SourceName { get; init; }

    public string? ResolvedValuePreview { get; init; }

    public string? CssVariableName { get; init; }

    public required string ReferenceValue { get; init; }

    public static DesignTokenPickerItemResponse From(DesignTokenPickerItem item) => new()
    {
        Path = item.Path,
        Type = item.Type,
        Label = item.Label,
        SourceType = item.SourceType,
        SourceName = item.SourceName,
        ResolvedValuePreview = item.ResolvedValuePreview,
        CssVariableName = item.CssVariableName,
        ReferenceValue = item.ReferenceValue
    };
}

public sealed class DesignTokenPreviewResponse
{
    public required bool Success { get; init; }

    public string PreviewCss { get; init; } = string.Empty;

    public string GeneratedCss { get; init; } = string.Empty;

    public string TailwindJson { get; init; } = string.Empty;

    public int TokenCount { get; init; }

    public DesignTokenBuildReportResponse? BuildReport { get; init; }

    public IReadOnlyList<DesignTokenDiagnosticResponse> Errors { get; init; } = [];

    public IReadOnlyList<DesignTokenDiagnosticResponse> Infos { get; init; } = [];

    public IReadOnlyList<DesignTokenDiagnosticResponse> Warnings { get; init; } = [];

    public IReadOnlyList<DesignTokenTokenListItemResponse> Tokens { get; init; } = [];

    public DesignTokenPreviewComparisonResponse Comparison { get; init; } = new();

    public IReadOnlyList<DesignTokenThemeVariantResponse> ThemeVariants { get; init; } = [];

    public IReadOnlyList<DesignTokenSourceSummaryResponse> SourceSummaries { get; init; } = [];

    public DesignTokenSourceSummaryTotalsResponse? SourceSummaryTotals { get; init; }

    public IReadOnlyList<DesignTokenMergeEventResponse> MergeEvents { get; init; } = [];

    public object? DiagnosticsExport { get; init; }

    public static DesignTokenPreviewResponse From(DesignTokenPreviewResult result) => new()
    {
        Success = result.Success,
        PreviewCss = result.PreviewCss,
        GeneratedCss = result.PreviewCss,
        TailwindJson = result.TailwindJson,
        TokenCount = result.TokenCount,
        BuildReport = result.BuildReport is null ? null : DesignTokenBuildReportResponse.From(result.BuildReport),
        Errors = result.Errors.Select(DesignTokenDiagnosticResponse.From).ToArray(),
        Infos = result.BuildReport?.Infos.Select(DesignTokenDiagnosticResponse.From).ToArray() ?? [],
        Warnings = result.Warnings.Select(DesignTokenDiagnosticResponse.From).ToArray(),
        Tokens = result.Tokens.Select(DesignTokenTokenListItemResponse.From).ToArray(),
        Comparison = DesignTokenPreviewComparisonResponse.From(result.Comparison),
        ThemeVariants = result.ThemeVariants.Select(DesignTokenThemeVariantResponse.From).ToArray(),
        SourceSummaries = result.SourceSummaries.Select(DesignTokenSourceSummaryResponse.From).ToArray(),
        SourceSummaryTotals = DesignTokenSourceSummaryTotalsResponse.From(result.SourceSummaryTotals),
        MergeEvents = result.MergeEvents.Select(DesignTokenMergeEventResponse.From).ToArray(),
        DiagnosticsExport = result.DiagnosticsExport
    };
}

public sealed class DesignTokenSourceSummaryResponse
{
    public required string SourceType { get; init; }
    public string? SourceName { get; init; }
    public required int Priority { get; init; }
    public required bool Enabled { get; init; }
    public int TokenCountBeforeMerge { get; init; }
    public int TokenCountAfterMerge { get; init; }
    public int TokensOverriddenByHigherSource { get; init; }
    public int Errors { get; init; }
    public int Warnings { get; init; }
    public int Info { get; init; }

    public static DesignTokenSourceSummaryResponse From(DesignTokenSourceSummary summary) => new()
    {
        SourceType = summary.SourceType.ToString(),
        SourceName = summary.SourceName,
        Priority = summary.Priority,
        Enabled = summary.Enabled,
        TokenCountBeforeMerge = summary.TokenCountBeforeMerge,
        TokenCountAfterMerge = summary.TokenCountAfterMerge,
        TokensOverriddenByHigherSource = summary.TokensOverriddenByHigherSource,
        Errors = summary.ErrorCount,
        Warnings = summary.WarningCount,
        Info = summary.InfoCount
    };
}

public sealed class DesignTokenSourceSummaryTotalsResponse
{
    public int TotalTokensBeforeMerge { get; init; }
    public int TotalTokensAfterMerge { get; init; }
    public int TokensAdded { get; init; }
    public int TokensOverridden { get; init; }
    public int SamePriorityDuplicates { get; init; }
    public int DisabledSources { get; init; }

    public static DesignTokenSourceSummaryTotalsResponse From(DesignTokenSourceSummaryTotals totals) => new()
    {
        TotalTokensBeforeMerge = totals.TotalTokensBeforeMerge,
        TotalTokensAfterMerge = totals.TotalTokensAfterMerge,
        TokensAdded = totals.TokensAdded,
        TokensOverridden = totals.TokensOverridden,
        SamePriorityDuplicates = totals.SamePriorityDuplicates,
        DisabledSources = totals.DisabledSources
    };
}

public sealed class DesignTokenMergeEventResponse
{
    public required string EventType { get; init; }
    public required string Message { get; init; }
    public string? TokenPath { get; init; }
    public string? SourceType { get; init; }
    public string? SourceName { get; init; }
    public int? SourcePriority { get; init; }

    public static DesignTokenMergeEventResponse From(DesignTokenMergeEvent mergeEvent) => new()
    {
        EventType = mergeEvent.EventType,
        Message = mergeEvent.Message,
        TokenPath = mergeEvent.TokenPath,
        SourceType = mergeEvent.SourceType,
        SourceName = mergeEvent.SourceName,
        SourcePriority = mergeEvent.SourcePriority
    };
}

public sealed class DesignTokenSourceDescriptorResponse
{
    public required string SourceType { get; init; }
    public string? SourceName { get; init; }
    public int SourcePriority { get; init; }
    public string? TokenType { get; init; }
    public string? RawValue { get; init; }

    public static DesignTokenSourceDescriptorResponse From(Site.DesignTokens.Sources.DesignTokenSourceDescriptor descriptor) => new()
    {
        SourceType = descriptor.SourceType.ToString(),
        SourceName = descriptor.SourceName,
        SourcePriority = descriptor.SourcePriority,
        TokenType = descriptor.TokenType,
        RawValue = descriptor.RawValue
    };
}

public sealed class DesignTokenThemeVariantResponse
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Alias { get; init; }

    public required string Selector { get; init; }

    public required bool IsDefault { get; init; }

    public required string VariantType { get; init; }

    public required bool Enabled { get; init; }

    public static DesignTokenThemeVariantResponse From(DesignTokenThemeVariantSummary variant) => new()
    {
        Id = variant.Id,
        Name = variant.Name,
        Alias = variant.Alias,
        Selector = variant.Selector,
        IsDefault = variant.IsDefault,
        VariantType = variant.VariantType,
        Enabled = variant.Enabled
    };
}

public sealed class DesignTokenPreviewComparisonResponse
{
    public int ChangedTokenCount { get; init; }

    public int AddedTokenCount { get; init; }

    public int RemovedTokenCount { get; init; }

    public IReadOnlyList<DesignTokenPreviewComparisonItemResponse> ChangedTokens { get; init; } = [];

    public IReadOnlyList<DesignTokenPreviewComparisonItemResponse> AddedTokens { get; init; } = [];

    public IReadOnlyList<DesignTokenPreviewComparisonItemResponse> RemovedTokens { get; init; } = [];

    public static DesignTokenPreviewComparisonResponse From(DesignTokenPreviewComparisonSummary summary) => new()
    {
        ChangedTokenCount = summary.ChangedTokenCount,
        AddedTokenCount = summary.AddedTokenCount,
        RemovedTokenCount = summary.RemovedTokenCount,
        ChangedTokens = summary.ChangedTokens.Select(DesignTokenPreviewComparisonItemResponse.From).ToArray(),
        AddedTokens = summary.AddedTokens.Select(DesignTokenPreviewComparisonItemResponse.From).ToArray(),
        RemovedTokens = summary.RemovedTokens.Select(DesignTokenPreviewComparisonItemResponse.From).ToArray()
    };
}

public sealed class DesignTokenPreviewComparisonItemResponse
{
    public required string Path { get; init; }

    public string? ActiveValue { get; init; }

    public string? DraftValue { get; init; }

    public required string ChangeType { get; init; }

    public static DesignTokenPreviewComparisonItemResponse From(DesignTokenPreviewComparisonItem item) => new()
    {
        Path = item.Path,
        ActiveValue = item.ActiveValue,
        DraftValue = item.DraftValue,
        ChangeType = item.ChangeType
    };
}
