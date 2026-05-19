namespace Site.DesignTokens.Tailwind;

public sealed class DesignTokenTailwindWriter : IDesignTokenTailwindWriter
{
    public DesignTokenTailwindWriter(string? outputPath = null)
    {
        OutputPath = string.IsNullOrWhiteSpace(outputPath)
            ? Path.Combine("App_Data", "DesignTokens", "generated-tailwind-theme.json")
            : outputPath;
    }

    public string OutputPath { get; }

    public void Write(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        var directory = Path.GetDirectoryName(OutputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = $"{OutputPath}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(tempPath, json);

        if (File.Exists(OutputPath))
        {
            File.Replace(tempPath, OutputPath, null);
            return;
        }

        File.Move(tempPath, OutputPath);
    }
}
