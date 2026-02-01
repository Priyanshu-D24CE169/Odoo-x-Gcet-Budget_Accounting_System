using System;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShivFurnitureERP.Data;
using ShivFurnitureERP.Infrastructure;
using ShivFurnitureERP.Middleware;
using ShivFurnitureERP.Models;
using ShivFurnitureERP.Options;
using ShivFurnitureERP.Repositories;
using ShivFurnitureERP.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var culture = CultureInfo.GetCultureInfo("en-IN");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

builder.Services.AddControllersWithViews(options =>
{
    options.Conventions.Add(new AreaAuthorizationConvention("Admin", "AdminOnly"));
    options.Conventions.Add(new AreaAuthorizationConvention("Portal", "PortalOnly"));
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireDigit = false;
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
    options.AddPolicy("PortalOnly", policy => policy.RequireRole("PortalUser"));
});

builder.Services.ConfigureApplicationCookie(options =>
{
    const int adminSessionMinutes = 20;
    const int portalSessionMinutes = 10;

    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Home/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(adminSessionMinutes);
    options.SlidingExpiration = false;
    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToLogin = context =>
        {
            var requestedPath = context.Request.Path + context.Request.QueryString;
            var redirectUrl = string.Concat(options.LoginPath, "?returnUrl=", Uri.EscapeDataString(requestedPath));
            context.Response.Redirect(redirectUrl);
            return Task.CompletedTask;
        },
        OnRedirectToAccessDenied = context =>
        {
            context.Response.Redirect("/Home/AccessDenied");
            return Task.CompletedTask;
        },
        OnSigningIn = context =>
        {
            var principal = context.Principal;
            if (principal is null)
            {
                return Task.CompletedTask;
            }

            double? minutes = principal.IsInRole("Admin")
                ? adminSessionMinutes
                : principal.IsInRole("PortalUser")
                    ? portalSessionMinutes
                    : null;

            if (minutes.HasValue)
            {
                var issued = DateTimeOffset.UtcNow;
                context.Properties.IssuedUtc = issued;
                context.Properties.ExpiresUtc = issued.AddMinutes(minutes.Value);
                context.Properties.AllowRefresh = false;

                var identity = principal.Identities.FirstOrDefault();
                if (identity is not null && context.Properties.ExpiresUtc.HasValue)
                {
                    const string claimType = "session-expiration";
                    var existingClaim = identity.FindFirst(claimType);
                    if (existingClaim is not null)
                    {
                        identity.RemoveClaim(existingClaim);
                    }

                    identity.AddClaim(new Claim(
                        claimType,
                        context.Properties.ExpiresUtc.Value.ToUnixTimeSeconds().ToString(),
                        ClaimValueTypes.Integer64));
                }
            }

            return Task.CompletedTask;
        }
    };
});

builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IContactService, ContactService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAnalyticalAccountService, AnalyticalAccountService>();
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<IEmailNotificationService, GmailEmailNotificationService>();
builder.Services.Configure<RazorpayOptions>(builder.Configuration.GetSection("Razorpay"));
builder.Services.AddScoped<RazorpayPaymentService>();
builder.Services.AddScoped<IAnalyticalRuleEngine, AnalyticalRuleEngine>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<IBudgetWarningService, BudgetWarningService>();
builder.Services.AddScoped<IVendorBillService, VendorBillService>();
builder.Services.AddScoped<IBillPaymentService, BillPaymentService>();
builder.Services.AddScoped<ISalesOrderService, SalesOrderService>();
builder.Services.AddScoped<ICustomerInvoiceService, CustomerInvoiceService>();
builder.Services.AddScoped<IInvoicePdfService, InvoicePdfService>();
builder.Services.AddSingleton<IPaymentGatewaySimulator, PaymentGatewaySimulator>();
builder.Services.AddScoped<IAnalyticalBudgetService, AnalyticalBudgetService>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var scopedProvider = scope.ServiceProvider;
    var dbContext = scopedProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
    await IdentitySeeder.SeedAsync(scopedProvider);
    await DomainDataSeeder.SeedAsync(scopedProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseGlobalExceptionHandling();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UsePasswordChangeEnforcement();
app.UseAuthorization();

app.MapAreaControllerRoute(
    name: "admin",
    areaName: "Admin",
    pattern: "Admin/{controller=Account}/{action=Login}/{id?}");

app.MapAreaControllerRoute(
    name: "portal",
    areaName: "Portal",
    pattern: "Portal/{controller=Account}/{action=Login}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

await app.RunAsync();
