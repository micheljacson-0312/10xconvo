using System.Security.Claims;

namespace TenXConvo.API.Helpers
{
    public static class AuthHelper
    {
        public static Guid GetUserId(ClaimsPrincipal user)
        {
            if (user == null)
                throw new UnauthorizedAccessException("User context not available.");

            var id =
                user.FindFirst("sub")?.Value ??
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(id))
                throw new UnauthorizedAccessException("UserId claim missing in token.");

            return Guid.Parse(id);
        }
    }
}