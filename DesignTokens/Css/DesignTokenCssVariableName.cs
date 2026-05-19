using System.Text.RegularExpressions;
using Site.DesignTokens.Models;

namespace Site.DesignTokens.Css;

internal static class DesignTokenCssVariableName
{
    private static readonly Regex CamelCaseBoundaryPattern = new("([a-z0-9])([A-Z])", RegexOptions.Compiled);
    private static readonly Regex ValidSegmentPattern = new("^[A-Za-z0-9-]+$", RegexOptions.Compiled);

    public static bool TryCreate(DesignToken token, out string name, out string error)
    {
        ArgumentNullException.ThrowIfNull(token);
        return TryCreate(token.Path, out name, out error);
    }

    public static bool TryCreate(DesignTokenPath path, out string name, out string error)
    {
        ArgumentNullException.ThrowIfNull(path);

        var segments = new List<string>();
        foreach (var segment in path.Segments)
        {
            var normalizedSegment = NormalizeSegment(segment);
            if (string.IsNullOrWhiteSpace(normalizedSegment) || !ValidSegmentPattern.IsMatch(normalizedSegment))
            {
                name = string.Empty;
                error = $"Invalid CSS variable name segment '{segment}'.";
                return false;
            }

            segments.Add(normalizedSegment);
        }

        name = $"--{string.Join("-", segments)}";
        error = string.Empty;
        return true;
    }

    private static string NormalizeSegment(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment))
        {
            return string.Empty;
        }

        return CamelCaseBoundaryPattern.Replace(segment.Trim(), "$1-$2").ToLowerInvariant();
    }
}
