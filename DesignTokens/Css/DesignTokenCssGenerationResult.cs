namespace Site.DesignTokens.Css;

public sealed class DesignTokenCssGenerationResult
{
    public DesignTokenCssGenerationResult(string css, IReadOnlyList<DesignTokenCssGenerationError> errors)
    {
        Css = css ?? string.Empty;
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));
    }

    public string Css { get; }

    public IReadOnlyList<DesignTokenCssGenerationError> Errors { get; }

    public bool Success => Errors.Count == 0;
}
