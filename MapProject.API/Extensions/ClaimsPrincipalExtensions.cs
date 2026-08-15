using System.Security.Claims;

namespace MapProject.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Token'daki "sub" claim'i kullanıcı id'sini taşıyor (AuthService koyuyor).
    /// ASP.NET bu claim'i okurken ClaimTypes.NameIdentifier'a eşliyor,
    /// bu yüzden iki isme de bakıyoruz.
    /// </summary>
    public static int GetUserId(this ClaimsPrincipal user)
    {
        var raw = user.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? user.FindFirstValue("sub");

        return int.TryParse(raw, out var id) ? id : 0;
    }
}
