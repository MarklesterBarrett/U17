using System.Security.Claims;

namespace Site.DesignTokens.Management;

public sealed class DesignTokenBackofficeAccessService : IDesignTokenBackofficeAccessService
{
    public bool HasAccess(ClaimsPrincipal? user) =>
        user?.Identity?.IsAuthenticated == true &&
        (user.IsInRole("admin") ||
         user.IsInRole("administrator") ||
         user.Claims.Any(x =>
             string.Equals(x.Type, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase) &&
             (string.Equals(x.Value, "admin", StringComparison.OrdinalIgnoreCase) ||
              string.Equals(x.Value, "administrator", StringComparison.OrdinalIgnoreCase))));
}
