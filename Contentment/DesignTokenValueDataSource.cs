using Site.DesignTokens;
using System.Text.Json;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Community.Contentment.DataEditors;

namespace Site.Contentment;

public sealed class DesignTokenValueDataSource : IContentmentDataSource
{
    private const string PrefixesConfigKey = "prefixes";
    private readonly IDesignTokenProvider _designTokenProvider;

    public DesignTokenValueDataSource(IDesignTokenProvider designTokenProvider)
    {
        _designTokenProvider = designTokenProvider;
    }

    public string Name => "Style Setting Values";

    public string Description => "Reads value tokens from site settings.";

    public string Icon => "icon-autofill";

    public string Group => "Custom";

    public OverlaySize OverlaySize => OverlaySize.Small;

    public Dictionary<string, object> DefaultValues => new();

    public IEnumerable<ContentmentConfigurationField> Fields => [];

    public IEnumerable<DataListItem> GetItems(Dictionary<string, object> config)
    {
        var prefixes = GetPrefixes(config);

        return _designTokenProvider
            .GetTokens()
            .Values
            .Where(x => prefixes.Count == 0 || prefixes.Any(prefix => x.Alias.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .Select(x => new DataListItem
            {
                Name = x.Label,
                Value = x.Alias,
                Description = x.Value
            });
    }

    private static IReadOnlyList<string> GetPrefixes(Dictionary<string, object> config)
    {
        if (config.TryGetValue(PrefixesConfigKey, out var rawValue) is false || rawValue is null)
        {
            return [];
        }

        if (rawValue is string text)
        {
            return text
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(x => string.IsNullOrWhiteSpace(x) is false)
                .ToArray();
        }

        if (rawValue is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                return element
                    .EnumerateArray()
                    .Where(x => x.ValueKind == JsonValueKind.String)
                    .Select(x => x.GetString())
                    .Where(x => string.IsNullOrWhiteSpace(x) is false)
                    .Cast<string>()
                    .ToArray();
            }

            if (element.ValueKind == JsonValueKind.String)
            {
                var value = element.GetString();
                return string.IsNullOrWhiteSpace(value)
                    ? []
                    : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
        }

        if (rawValue is IEnumerable<object> values)
        {
            return values
                .Select(x => x?.ToString())
                .Where(x => string.IsNullOrWhiteSpace(x) is false)
                .Cast<string>()
                .ToArray();
        }

        return [];
    }
}
