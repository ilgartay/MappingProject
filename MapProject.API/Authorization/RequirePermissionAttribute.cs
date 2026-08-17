using MapProject.API.Extensions;
using MapProject.Business.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MapProject.API.Authorization;

/// <summary>
/// Belirli bir yetki kodunu şart koşar.
/// [Authorize] "giriş yapmış mı" sorusunu cevaplıyor; bu filtre "bu işlemi
/// yapma yetkisi var mı" sorusunu. Arayüzde menüyü gizlemek yeterli değil:
/// istek doğrudan da atılabilir, karar sunucuda verilmeli.
/// </summary>
public class RequirePermissionAttribute : TypeFilterAttribute
{
    public RequirePermissionAttribute(string permission)
        : base(typeof(RequirePermissionFilter))
    {
        Arguments = [permission];
    }

    private sealed class RequirePermissionFilter : IAsyncAuthorizationFilter
    {
        private readonly string _permission;
        private readonly IUserService _userService;

        public RequirePermissionFilter(string permission, IUserService userService)
        {
            _permission = permission;
            _userService = userService;
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var userId = context.HttpContext.User.GetUserId();
            var current = await _userService.GetCurrentAsync(userId);

            if (current is null || !current.Permissions.Contains(_permission))
            {
                // 401 değil 403: kullanıcı tanınıyor ama bu işleme yetkisi yok.
                context.Result = new ObjectResult(new { message = "Bu işlem için yetkiniz yok." })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
            }
        }
    }
}
