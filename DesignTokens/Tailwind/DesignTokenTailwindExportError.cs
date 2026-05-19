namespace Site.DesignTokens.Tailwind;

public sealed class DesignTokenTailwindExportError
{
    public DesignTokenTailwindExportError(string path, string message)
    {
        Path = path ?? string.Empty;
        Message = message ?? string.Empty;
    }

    public string Path { get; }

    public string Message { get; }
}
