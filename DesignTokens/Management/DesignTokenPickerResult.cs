namespace Site.DesignTokens.Management;

public sealed class DesignTokenPickerResult
{
    public required bool HasActiveBuild { get; init; }

    public string? EmptyMessage { get; init; }

    public string? AppliedContext { get; init; }

    public string? AppliedTokenType { get; init; }

    public IReadOnlyList<DesignTokenPickerItem> Items { get; init; } = [];
}
