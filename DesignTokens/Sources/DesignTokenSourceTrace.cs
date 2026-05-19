namespace Site.DesignTokens.Sources;

public sealed class DesignTokenSourceTrace
{
    public DesignTokenSourceTrace(
        string tokenPath,
        DesignTokenSourceDescriptor winningSource,
        IReadOnlyList<DesignTokenSourceDescriptor> overriddenSources)
    {
        TokenPath = tokenPath;
        WinningSource = winningSource;
        OverriddenSources = overriddenSources;
    }

    public string TokenPath { get; }

    public DesignTokenSourceDescriptor WinningSource { get; }

    public IReadOnlyList<DesignTokenSourceDescriptor> OverriddenSources { get; }
}
