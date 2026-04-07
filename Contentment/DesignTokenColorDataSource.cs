using System.Text.Json;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Community.Contentment.DataEditors;

namespace Site.Contentment;

public sealed class DesignTokenColorDataSource : IContentmentDataSource
{
    private readonly IWebHostEnvironment _environment;

    public DesignTokenColorDataSource(IWebHostEnvironment environment)
    {
        _environment = environment;
    }

    public string Name => "Design Token Colors";

    public string Description => "Reads color tokens from a design-token JSON file.";

    public string Icon => "icon-colorpicker";

    public string Group => "Custom";

    public OverlaySize OverlaySize => OverlaySize.Small;

    public Dictionary<string, object> DefaultValues => new()
    {
        ["filePath"] = "/App_Plugins/DesignTokens/tokens/colors.json"
    };

    public IEnumerable<ContentmentConfigurationField> Fields => new[]
    {
        new ContentmentConfigurationField
        {
            Key = "filePath",
            Name = "JSON file path",
            Description = "Site-relative path to the design token JSON file.",
            PropertyEditorUiAlias = "Umb.PropertyEditorUi.TextBox"
        }
    };

    public IEnumerable<DataListItem> GetItems(Dictionary<string, object> config)
    {
        if (!config.TryGetValue("filePath", out var filePathObj))
        {
            return Enumerable.Empty<DataListItem>();
        }

        var filePath = filePathObj?.ToString();
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return Enumerable.Empty<DataListItem>();
        }

        var relativePath = filePath.TrimStart('/', '\\')
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
        var absolutePath = Path.Combine(_environment.WebRootPath, relativePath);
        if (string.IsNullOrWhiteSpace(absolutePath) || !File.Exists(absolutePath))
        {
            return Enumerable.Empty<DataListItem>();
        }

        using var stream = File.OpenRead(absolutePath);
        using var document = JsonDocument.Parse(stream);

        var items = new List<DataListItem>();
        AddColorTokenItems(document.RootElement, [], items);

        return items;
    }

    private static void AddColorTokenItems(JsonElement element, List<string> path, List<DataListItem> items)
    {
        if (IsColorToken(element, out var hex))
        {
            var fallbackTokenKey = string.Join("-", path);
            var tokenKey = GetAlias(element) ?? fallbackTokenKey;
            var label = GetLabel(element) ?? ToDisplayName(tokenKey);

            if (!string.IsNullOrWhiteSpace(tokenKey))
            {
                items.Add(new DataListItem
                {
                    Name = label,
                    Value = tokenKey,
                    Description = hex
                });
            }

            return;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var property in element.EnumerateObject())
        {
            path.Add(property.Name);
            AddColorTokenItems(property.Value, path, items);
            path.RemoveAt(path.Count - 1);
        }
    }

    private static bool IsColorToken(JsonElement element, out string? hex)
    {
        hex = null;

        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        if (!element.TryGetProperty("$type", out var typeProperty) ||
            !string.Equals(typeProperty.GetString(), "color", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!element.TryGetProperty("$value", out var valueProperty))
        {
            return false;
        }

        hex = valueProperty.GetString();
        return !string.IsNullOrWhiteSpace(hex);
    }

    private static string ToDisplayName(string tokenKey)
    {
        return string.Join(" ", tokenKey
            .Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }

    private static string? GetAlias(JsonElement element)
    {
        return GetExtensionString(element, "alias");
    }

    private static string? GetLabel(JsonElement element)
    {
        return GetExtensionString(element, "label");
    }

    private static string? GetExtensionString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty("$extensions", out var extensions) || extensions.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!extensions.TryGetProperty("site", out var siteExtension) || siteExtension.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (!siteExtension.TryGetProperty(propertyName, out var property))
        {
            return null;
        }

        return property.GetString();
    }
}
