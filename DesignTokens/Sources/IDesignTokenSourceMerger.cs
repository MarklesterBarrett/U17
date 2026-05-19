using Site.DesignTokens.Models;

namespace Site.DesignTokens.Sources;

public interface IDesignTokenSourceMerger
{
    DesignTokenSourceMergeResult Merge(IEnumerable<DesignTokenSource> sources);
}
