using Site.DesignTokens;

namespace Site.SubsidiarySites;

public sealed record SiteGenerationRequest(
    string SiteName,
    string? DesignTokenJson,
    IReadOnlyList<UploadedLogoAsset> LogoFiles);

public sealed record UploadedLogoAsset(
    string FileName,
    string ContentType,
    byte[] Bytes);

public sealed record UploadedLogoAssetRequest(
    string FileName,
    string ContentType,
    string Base64);

public sealed record SiteGenerationResponse(
    bool Success,
    string? NewSiteName,
    string? TenantKey,
    string? PreviewDomain,
    string? StylesheetUrl,
    IReadOnlyList<SiteGenerationStepResult> Steps,
    IReadOnlyList<string> CreatedItems,
    IReadOnlyList<string> UserDetailsToCopy,
    IReadOnlyList<string> ManualActions,
    IReadOnlyList<string> Warnings);

public sealed record SiteGenerationStepResult(
    string Alias,
    string Name,
    string Status,
    IReadOnlyList<string> Messages,
    IReadOnlyList<string> Errors);

public sealed record ThemeValidationResponse(
    bool IsValid,
    string Json,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings);

public sealed record ThemeImportResult(
    SiteThemeImport Theme,
    IReadOnlyList<string> Errors,
    IReadOnlyList<string> Warnings)
{
    public bool IsValid => Errors.Count == 0;
}

public sealed record SiteThemeImport(
    IReadOnlyDictionary<string, PrimitiveColorToken> Colors,
    IReadOnlyDictionary<string, ResponsiveTokenValue> CoreSpacing,
    IReadOnlyList<AdditionalSpacingToken> AdditionalSpacing,
    FontFamilyToken SansFont,
    FontFamilyToken DisplayFont,
    IReadOnlyDictionary<string, ResponsiveTokenValue> FontSizes,
    IReadOnlyDictionary<string, string> LineHeights,
    ResponsiveTokenValue LayoutGutter,
    IReadOnlyDictionary<string, string> LayoutWidths,
    IReadOnlyDictionary<string, string> Radius,
    IReadOnlyDictionary<string, string> Shadow,
    IReadOnlyDictionary<string, string> SemanticColorAssignments,
    IReadOnlyDictionary<string, string> SemanticValueAssignments,
    IReadOnlyDictionary<string, string> DirectValues,
    IReadOnlyList<AdditionalValueToken> AdditionalValueTokens);

public sealed record PrimitiveColorToken(string Alias, string Value);

public sealed record ResponsiveTokenValue(string Mobile, string Tablet, string Desktop);

public sealed record AdditionalSpacingToken(string Name, ResponsiveTokenValue Value);

public sealed record FontFamilyToken(string Preset, string CustomValue);

public sealed record AdditionalValueToken(string Alias, string Label, string Value);

internal sealed record LogoAssignmentResult(
    Guid? FolderKey,
    Guid? HeaderLogoMediaKey,
    Guid? FooterLogoMediaKey,
    Guid? FaviconMediaKey,
    IReadOnlyList<string> Messages);
