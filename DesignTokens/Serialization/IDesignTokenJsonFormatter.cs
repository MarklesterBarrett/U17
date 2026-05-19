using Site.DesignTokens.Models;

namespace Site.DesignTokens.Serialization;

public interface IDesignTokenJsonFormatter
{
    string Format(string json);

    string FormatRegistry(DesignTokenRegistry registry, bool useResolvedValues);
}
