using Site.DesignTokens.Diagnostics;
using Site.DesignTokens.Persistence;

namespace Site.DesignTokens.Management;

public interface IDesignTokenManagementService
{
    DesignTokenStatusSnapshot GetStatus();

    DesignTokenDocument? GetActiveDocument();

    string ExportActiveJson();

    DesignTokenDiagnosticsResult Validate(string json);

    DesignTokenManagementResult SaveDraft(string json, string? name, string? user);

    DesignTokenManagementResult Activate(Guid documentId, string? user);

    DesignTokenManagementResult Rebuild(string? user);

    IReadOnlyList<DesignTokenDiagnosticViewModel> GetTokens();

    DesignTokenDiagnosticViewModel? GetToken(string path);

    DesignTokenPreviewResult PreviewBuild(string json);

    DesignTokenPreviewResult PreviewValidate(string json);

    DesignTokenPickerResult GetPickerItems(string? query = null, string? tokenType = null, string? sourceType = null, string? context = null, int limit = 100);

    DesignTokenPickerItem? GetPickerItem(string path);
}
