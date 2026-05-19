using System.Security.Claims;

namespace Site.DesignTokens.Management;

public interface IDesignTokenBackofficeAccessService
{
    bool HasAccess(ClaimsPrincipal? user);
}
