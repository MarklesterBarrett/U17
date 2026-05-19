using System.Text.Json;

namespace Site.DesignTokens.Management;

public sealed class DesignTokenBuildStatusStore : IDesignTokenBuildStatusStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    public DesignTokenBuildStatusStore(string? storePath = null)
    {
        StorePath = string.IsNullOrWhiteSpace(storePath)
            ? Path.Combine("App_Data", "DesignTokens", "build-status.json")
            : storePath;
    }

    public string StorePath { get; }

    public DesignTokenBuildStatusRecord? Get()
    {
        if (!File.Exists(StorePath))
        {
            return null;
        }

        var json = File.ReadAllText(StorePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<DesignTokenBuildStatusRecord>(json, SerializerOptions);
    }

    public void Save(DesignTokenBuildStatusRecord status)
    {
        ArgumentNullException.ThrowIfNull(status);

        var directory = Path.GetDirectoryName(StorePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(status, SerializerOptions);
        var tempPath = $"{StorePath}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(tempPath, json);

        if (File.Exists(StorePath))
        {
            File.Replace(tempPath, StorePath, null);
            return;
        }

        File.Move(tempPath, StorePath);
    }
}
