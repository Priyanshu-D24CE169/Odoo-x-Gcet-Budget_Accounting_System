using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using ShivFurnitureERP.Models;

namespace ShivFurnitureERP.Middleware;

public class PasswordChangeEnforcementMiddleware
{
    private static readonly PathString ForcePasswordPath = new("/Portal/Account/ForcePasswordChange");
    private readonly RequestDelegate _next;

    public PasswordChangeEnforcementMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            context.User.IsInRole("PortalUser") &&
            context.Request.Path.StartsWithSegments("/Portal", StringComparison.OrdinalIgnoreCase) &&
            !context.Request.Path.StartsWithSegments(ForcePasswordPath, StringComparison.OrdinalIgnoreCase) &&
            !context.Request.Path.StartsWithSegments("/Portal/Account/Login", StringComparison.OrdinalIgnoreCase) &&
            !context.Request.Path.StartsWithSegments("/Portal/Account/Logout", StringComparison.OrdinalIgnoreCase))
        {
            var user = await userManager.GetUserAsync(context.User);
            if (user?.MustChangePassword == true)
            {
                var returnUrl = context.Request.Path + context.Request.QueryString;
                var redirectUrl = $"/Portal/Account/ForcePasswordChange?returnUrl={Uri.EscapeDataString(returnUrl)}";
                context.Response.Redirect(redirectUrl);
                return;
            }
        }

        await _next(context);
    }
}

public static class PasswordChangeEnforcementMiddlewareExtensions
{
    public static IApplicationBuilder UsePasswordChangeEnforcement(this IApplicationBuilder app)
    {
        return app.UseMiddleware<PasswordChangeEnforcementMiddleware>();
    }
}
