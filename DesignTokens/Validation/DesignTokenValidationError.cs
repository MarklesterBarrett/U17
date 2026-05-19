using Site.DesignTokens.Models;

namespace Site.DesignTokens.Validation;

public sealed record DesignTokenValidationError(
    string Path,
    DesignTokenType Type,
    string? Field,
    string Message);
