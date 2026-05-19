using Site.DesignTokens.Models;

namespace Site.DesignTokens.Tailwind;

public interface IDesignTokenTailwindExporter
{
    DesignTokenTailwindExportResult Export(DesignTokenRegistry registry);
}
