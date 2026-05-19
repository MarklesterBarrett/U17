namespace Site.DesignTokens.Css;

public sealed class DesignTokenCssWriter : IDesignTokenCssWriter
{
    public DesignTokenCssWriter(string? outputPath = null)
    {
        OutputPath = string.IsNullOrWhiteSpace(outputPath)
            ? Path.Combine("css", "generated-tokens.css")
            : outputPath;
    }

    public string OutputPath { get; }

    public void Write(string css)
    {
        ArgumentNullException.ThrowIfNull(css);

        var targetPath = OutputPath;
        var directory = Path.GetDirectoryName(targetPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = $"{targetPath}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(tempPath, css);

        if (File.Exists(targetPath))
        {
            File.Replace(tempPath, targetPath, null);
            return;
        }

        File.Move(tempPath, targetPath);
    }
}
