using Site.DesignTokens;
using Site.DesignTokens.Defaults;
using Site.DesignTokens.Persistence;
using Site.DesignTokens.Serialization;

namespace Site.DesignTokens.Export;

public sealed class DesignTokenExportService : IDesignTokenExportService
{
    private readonly IDesignTokenDocumentStore _documentStore;
    private readonly DesignTokenBuildPipeline _buildPipeline;
    private readonly IDesignTokenStarterJsonProvider _starterJsonProvider;
    private readonly IDesignTokenJsonFormatter _jsonFormatter;

    public DesignTokenExportService(
        IDesignTokenDocumentStore documentStore,
        DesignTokenBuildPipeline buildPipeline,
        IDesignTokenStarterJsonProvider starterJsonProvider,
        IDesignTokenJsonFormatter jsonFormatter)
    {
        _documentStore = documentStore;
        _buildPipeline = buildPipeline;
        _starterJsonProvider = starterJsonProvider;
        _jsonFormatter = jsonFormatter;
    }

    public string Export(DesignTokenExportMode mode)
    {
        return mode switch
        {
            DesignTokenExportMode.ActiveImportedOnly => ExportActiveImportedOnly(),
            DesignTokenExportMode.MergedResolved => ExportMergedResolved(),
            DesignTokenExportMode.Starter => _starterJsonProvider.GetStarterJson(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
    }

    private string ExportActiveImportedOnly()
    {
        var document = _documentStore.GetActive();
        return document?.Json ?? string.Empty;
    }

    private string ExportMergedResolved()
    {
        var result = _buildPipeline.Build((string?)null);
        return result.Success
            ? _jsonFormatter.FormatRegistry(result.Registry, useResolvedValues: true)
            : string.Empty;
    }
}
