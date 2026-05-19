using Site.DesignTokens.Diagnostics;

namespace Site.DesignTokens.Management;

public interface IDesignTokenOutputWriter
{
    IReadOnlyList<DesignTokenDiagnostic> Write(string css, string tailwindJson);
}
