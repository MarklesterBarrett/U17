using Site.DesignTokens.Sources;

namespace Site.DesignTokens.Diagnostics;

public sealed class DesignTokenDiagnosticViewModel
{
    public required string Path { get; init; }

    public required string Type { get; init; }

    public required DesignTokenSourceType SourceType { get; init; }

    public string? SourceName { get; init; }

    public int SourcePriority { get; init; }

    public string? RawValue { get; init; }

    public string? NormalizedValue { get; init; }

    public string? ResolvedValue { get; init; }

    public string? GeneratedCssVariableName { get; init; }

    public string? GeneratedCssValue { get; init; }

    public IReadOnlyList<string> References { get; init; } = [];

    public IReadOnlyList<string> ReferencedBy { get; init; } = [];

    public IReadOnlyList<string> Errors { get; init; } = [];

    public IReadOnlyList<string> ValidationErrors { get; init; } = [];

    public IReadOnlyList<string> Warnings { get; init; } = [];

    public IReadOnlyList<string> Infos { get; init; } = [];

    public IReadOnlyList<DesignTokenSourceDescriptor> OverriddenSources { get; init; } = [];

    public IReadOnlyList<string> ResolutionTrace { get; init; } = [];

    public bool IsAdded { get; init; }

    public bool IsOverridden { get; init; }

    public bool IsUnused { get; init; }

    public bool HasGeneratedCss { get; init; }

    public bool IsTailwindMapped { get; init; }

    public bool IsTailwindSkipped { get; init; }
}
