namespace Site.DesignTokens.Diagnostics;

public sealed class DesignTokenDiagnosticsExport
{
    public IReadOnlyList<object> Themes { get; init; } = [];

    public IReadOnlyList<object> Sources { get; init; } = [];

    public IReadOnlyList<object> Tokens { get; init; } = [];

    public IReadOnlyList<object> MergeEvents { get; init; } = [];

    public IReadOnlyList<object> Infos { get; init; } = [];

    public IReadOnlyList<object> Warnings { get; init; } = [];

    public IReadOnlyList<object> Errors { get; init; } = [];
}
