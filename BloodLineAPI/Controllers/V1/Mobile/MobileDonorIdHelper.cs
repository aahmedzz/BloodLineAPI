using System.Security.Claims;

namespace BloodLineAPI.Controllers.V1.Mobile;

internal static class MobileDonorIdHelper
{
    public static Guid? TryGetDonorId(ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : null;
    }
}
