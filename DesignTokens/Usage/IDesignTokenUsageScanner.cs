using Site.DesignTokens.Models;

namespace Site.DesignTokens.Usage;

public interface IDesignTokenUsageScanner
{
    DesignTokenUsageScanResult Scan(DesignTokenRegistry registry);
}
