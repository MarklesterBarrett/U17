namespace Site.DesignTokens.References;

public sealed record DesignTokenReference(
    string SourcePath,
    string TargetPath,
    string RawReference);
