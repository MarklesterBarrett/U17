namespace Site.DesignTokens.Import;

public interface IDesignTokenImportService
{
    DesignTokenImportResult Import(string json, string? name = null, string? user = null, bool activate = false);
}
