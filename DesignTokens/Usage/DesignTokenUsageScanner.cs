using System.Text.RegularExpressions;
using Site.DesignTokens.Css;
using Site.DesignTokens.Models;

namespace Site.DesignTokens.Usage;

public sealed class DesignTokenUsageScanner : IDesignTokenUsageScanner
{
    private static readonly Regex CssVariableUsagePattern = new(@"var\(\s*(--[A-Za-z0-9-]+)\s*(?:,|\))", RegexOptions.Compiled);
    private static readonly Regex TokenReferencePattern = new(@"\{(?<path>[A-Za-z0-9._-]+)\}", RegexOptions.Compiled);
    private static readonly Regex HexColorPattern = new(@"#(?:[0-9a-fA-F]{8}|[0-9a-fA-F]{6}|[0-9a-fA-F]{3})\b", RegexOptions.Compiled);
    private static readonly Regex ColorFunctionPattern = new(@"\b(?:rgb|rgba|hsl|hsla)\([^)]+\)", RegexOptions.Compiled);
    private static readonly Regex LengthPattern = new(@"(?<![-\w])(?:\d+|\d*\.\d+)(?:px|rem|em|vw|vh|%)\b", RegexOptions.Compiled);
    private static readonly Regex DurationPattern = new(@"(?<![-\w])(?:\d+|\d*\.\d+)(?:ms|s)\b", RegexOptions.Compiled);

    private readonly DesignTokenUsageScannerOptions _options;
    private readonly DesignTokenCssVariablePathMapper _mapper;

    public DesignTokenUsageScanner(
        DesignTokenUsageScannerOptions? options = null,
        DesignTokenCssVariablePathMapper? mapper = null)
    {
        _options = options ?? new DesignTokenUsageScannerOptions();
        _mapper = mapper ?? new DesignTokenCssVariablePathMapper();
    }

    public DesignTokenUsageScanResult Scan(DesignTokenRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(registry);

        if (!_options.Enabled || string.IsNullOrWhiteSpace(_options.RootPath) || !Directory.Exists(_options.RootPath))
        {
            return new DesignTokenUsageScanResult
            {
                Enabled = _options.Enabled,
                RootPath = _options.RootPath
            };
        }

        var items = new List<DesignTokenUsageItem>();
        var usedTokenPaths = new HashSet<string>(StringComparer.Ordinal);
        var scannedFileCount = 0;

        foreach (var filePath in EnumerateFiles(_options.RootPath))
        {
            scannedFileCount++;
            ScanFile(filePath, registry, items, usedTokenPaths);
        }

        var unusedTokenPaths = registry.All
            .Select(x => x.Path.Value)
            .Where(x => !usedTokenPaths.Contains(x))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        foreach (var unusedTokenPath in unusedTokenPaths)
        {
            if (!registry.TryGet(unusedTokenPath, out var token) ||
                !DesignTokenCssVariableName.TryCreate(token!, out var cssVariableName, out _))
            {
                cssVariableName = null;
            }

            items.Add(new DesignTokenUsageItem
            {
                Kind = DesignTokenUsageKind.UnusedGeneratedToken,
                TokenPath = unusedTokenPath,
                CssVariableName = cssVariableName,
                Message = $"Generated token '{unusedTokenPath}' is not referenced in scanned files."
            });
        }

        return new DesignTokenUsageScanResult
        {
            Enabled = true,
            RootPath = _options.RootPath,
            Items = items,
            UsedTokenPaths = usedTokenPaths.OrderBy(x => x, StringComparer.Ordinal).ToArray(),
            UnusedTokenPaths = unusedTokenPaths,
            ScannedFileCount = scannedFileCount
        };
    }

    private void ScanFile(
        string filePath,
        DesignTokenRegistry registry,
        List<DesignTokenUsageItem> items,
        HashSet<string> usedTokenPaths)
    {
        var lines = File.ReadAllLines(filePath);

        for (var index = 0; index < lines.Length; index++)
        {
            var line = lines[index];
            var lineNumber = index + 1;

            foreach (Match match in TokenReferencePattern.Matches(line))
            {
                var tokenPath = match.Groups["path"].Value;
                if (registry.TryGet(tokenPath, out _))
                {
                    usedTokenPaths.Add(tokenPath);
                    items.Add(new DesignTokenUsageItem
                    {
                        Kind = DesignTokenUsageKind.TokenReferenceUsed,
                        TokenPath = tokenPath,
                        FilePath = filePath,
                        LineNumber = lineNumber,
                        Message = $"Token reference '{{{tokenPath}}}' used."
                    });
                }
                else
                {
                    items.Add(new DesignTokenUsageItem
                    {
                        Kind = DesignTokenUsageKind.MissingTokenReference,
                        TokenPath = tokenPath,
                        FilePath = filePath,
                        LineNumber = lineNumber,
                        Message = $"Token reference '{{{tokenPath}}}' does not exist in the active registry."
                    });
                }
            }

            foreach (Match match in CssVariableUsagePattern.Matches(line))
            {
                var cssVariableName = match.Groups[1].Value;
                if (_mapper.TryMap(cssVariableName, registry, out var tokenPath))
                {
                    usedTokenPaths.Add(tokenPath);
                    items.Add(new DesignTokenUsageItem
                    {
                        Kind = DesignTokenUsageKind.CssVariableUsed,
                        TokenPath = tokenPath,
                        CssVariableName = cssVariableName,
                        FilePath = filePath,
                        LineNumber = lineNumber,
                        Message = $"CSS variable '{cssVariableName}' maps to token '{tokenPath}'."
                    });
                }
                else
                {
                    items.Add(new DesignTokenUsageItem
                    {
                        Kind = DesignTokenUsageKind.MissingGeneratedCssVariable,
                        CssVariableName = cssVariableName,
                        FilePath = filePath,
                        LineNumber = lineNumber,
                        Message = $"CSS variable '{cssVariableName}' is referenced in code but is not generated by the active token registry."
                    });
                }
            }

            foreach (var hardcoded in FindHardcodedValues(line))
            {
                items.Add(new DesignTokenUsageItem
                {
                    Kind = DesignTokenUsageKind.HardcodedDesignValue,
                    FilePath = filePath,
                    LineNumber = lineNumber,
                    Message = $"Hardcoded design value '{hardcoded}' detected."
                });
            }
        }
    }

    private IEnumerable<string> EnumerateFiles(string rootPath)
    {
        var allowedExtensions = new HashSet<string>(_options.IncludeExtensions, StringComparer.OrdinalIgnoreCase);
        var excludedDirectories = new HashSet<string>(_options.ExcludedDirectories, StringComparer.OrdinalIgnoreCase);

        var pending = new Stack<string>();
        pending.Push(rootPath);

        while (pending.Count > 0)
        {
            var current = pending.Pop();

            foreach (var directory in Directory.EnumerateDirectories(current))
            {
                var name = Path.GetFileName(directory);
                if (excludedDirectories.Contains(name))
                {
                    continue;
                }

                pending.Push(directory);
            }

            foreach (var file in Directory.EnumerateFiles(current))
            {
                if (!allowedExtensions.Contains(Path.GetExtension(file)))
                {
                    continue;
                }

                if (_options.ExcludedFileNamePatterns.Any(pattern => file.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                yield return file;
            }
        }
    }

    private static IEnumerable<string> FindHardcodedValues(string line)
    {
        foreach (Match match in HexColorPattern.Matches(line))
        {
            yield return match.Value;
        }

        foreach (Match match in ColorFunctionPattern.Matches(line))
        {
            yield return match.Value;
        }

        foreach (Match match in LengthPattern.Matches(line))
        {
            yield return match.Value;
        }

        foreach (Match match in DurationPattern.Matches(line))
        {
            yield return match.Value;
        }
    }
}
