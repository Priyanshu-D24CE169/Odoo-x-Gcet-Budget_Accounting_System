using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Gcet.Middleware
{
    /// <summary>
    /// Redirects users flagged for password resets to the change password page regardless of requested URL.
    /// </summary>
    public class PasswordChangeEnforcementMiddleware
    {
        private readonly RequestDelegate _next;

        public PasswordChangeEnforcementMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated == true &&
                context.User.HasClaim(c => c.Type == "MustChangePassword"))
            {
                var path = context.Request.Path;
                var isChangePasswordPath = path.StartsWithSegments("/Account/ChangePassword", StringComparison.OrdinalIgnoreCase);
                var isLogout = path.StartsWithSegments("/Authentication/Logout", StringComparison.OrdinalIgnoreCase);

                if (!isChangePasswordPath && !isLogout && !path.StartsWithSegments("/css", StringComparison.OrdinalIgnoreCase) &&
                    !path.StartsWithSegments("/js", StringComparison.OrdinalIgnoreCase) &&
                    !path.StartsWithSegments("/lib", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Redirect("/Account/ChangePassword");
                    return;
                }
            }

            await _next(context);
        }
    }
}
