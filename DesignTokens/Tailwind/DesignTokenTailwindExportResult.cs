namespace Site.DesignTokens.Tailwind;

public sealed class DesignTokenTailwindExportResult
{
    public DesignTokenTailwindExportResult(
        string json,
        IReadOnlyList<DesignTokenTailwindExportError> errors)
    {
        Json = json;
        Errors = errors;
    }

    public string Json { get; }

    public IReadOnlyList<DesignTokenTailwindExportError> Errors { get; }

    public bool Success => Errors.Count == 0;
}
