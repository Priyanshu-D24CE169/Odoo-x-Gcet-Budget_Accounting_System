using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Budget_Accounting_System.Data;
using Budget_Accounting_System.Services;
using Budget_Accounting_System.Models;
using Budget_Accounting_System.Authorization;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Configure Entity Framework Core with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure application cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(24);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// Register application services
builder.Services.AddScoped<IAnalyticalAccountService, AnalyticalAccountService>();
builder.Services.AddScoped<IBudgetService, BudgetService>();
builder.Services.AddScoped<IBudgetActualService, BudgetActualService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Register authorization handlers
builder.Services.AddScoped<IAuthorizationHandler, ContactOwnerAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, InvoiceOwnerAuthorizationHandler>();
builder.Services.AddScoped<IAuthorizationHandler, BillOwnerAuthorizationHandler>();

// Add authorization policies for areas
builder.Services.AddAuthorization(options =>
{
    // Admin area policy
    options.AddPolicy("RequireAdminRole", policy =>
        policy.RequireRole(UserRoles.Admin));

    // Portal area policy
    options.AddPolicy("RequirePortalUserRole", policy =>
        policy.RequireRole(UserRoles.PortalUser));

    // Default policy - require authenticated user
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// Configure Razor Pages with Areas support
builder.Services.AddRazorPages(options =>
{
    // Require authentication for all pages by default
    options.Conventions.AuthorizeFolder("/");
    
    // Allow anonymous access to login and error pages
    options.Conventions.AllowAnonymousToPage("/Account/Login");
    options.Conventions.AllowAnonymousToPage("/Account/AccessDenied");
    options.Conventions.AllowAnonymousToPage("/Error");

    // Admin area - require Admin role for all pages except login
    options.Conventions.AuthorizeAreaFolder("Admin", "/", "RequireAdminRole");
    options.Conventions.AllowAnonymousToAreaPage("Admin", "/Account/Login");

    // Portal area - require PortalUser role for all pages except login
    options.Conventions.AuthorizeAreaFolder("Portal", "/", "RequirePortalUserRole");
    options.Conventions.AllowAnonymousToAreaPage("Portal", "/Account/Login");
});



var app = builder.Build();

// Initialize roles and default admin user
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
        try
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            await IdentityInitializer.SeedRolesAndUsersAsync(roleManager, userManager);
        }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding roles and admin user.");
    }
}

// Seed database with sample data
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await DataSeeder.SeedData(context);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

app.Run();


