namespace Site.DesignTokens.Usage;

public sealed class DesignTokenUsageScannerOptions
{
    public bool Enabled { get; init; }

    public string RootPath { get; init; } = Directory.GetCurrentDirectory();

    public IReadOnlyList<string> IncludeExtensions { get; init; } = [".cshtml", ".css", ".scss", ".js", ".ts", ".json"];

    public IReadOnlyList<string> ExcludedDirectories { get; init; } =
    [
        ".git",
        ".artifacts",
        ".tools",
        "bin",
        "obj",
        "node_modules",
        "umbraco",
        "App_Data",
        "dist",
        "build",
        "vendor"
    ];

    public IReadOnlyList<string> ExcludedFileNamePatterns { get; init; } =
    [
        "generated-tokens.css",
        "generated-tailwind-theme.json"
    ];
}
