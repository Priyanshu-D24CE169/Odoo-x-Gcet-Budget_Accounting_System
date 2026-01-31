using System;
using System.IO;
using Gcet.Data;
using Gcet.Middleware;
using Gcet.Options;
using Gcet.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography.X509Certificates;

public partial class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddDbContext<GcetDbContext>(options =>
            options.UseSqlServer(builder.Configuration.GetConnectionString("GcetDatabase")));

        builder.Services.AddScoped<IPasswordHasherService, PasswordHasherService>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
        builder.Services.AddScoped<IEmailService, EmailService>();
        var certificateSettings = builder.Configuration.GetSection("CertificateSettings").Get<CertificateSettings>() ?? new CertificateSettings();

        builder.Services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
        });

        builder.Services.AddControllersWithViews(options =>
        {
            options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
        });

        builder.Services.AddSession(options =>
        {
            options.Cookie.Name = "Gcet.Session";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Strict;
            options.IdleTimeout = TimeSpan.FromMinutes(20);
        });

        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = "Gcet.Auth";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Strict;
                options.LoginPath = "/Authentication/Login";
                options.AccessDeniedPath = "/Authentication/AccessDenied";
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
            });

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(5278);
            options.ListenAnyIP(7040, listenOptions =>
            {
                var certificate = LoadCertificate(certificateSettings, builder.Environment.ContentRootPath);
                if (certificate != null)
                {
                    listenOptions.UseHttps(certificate);
                }
                else
                {
                    listenOptions.UseHttps();
                }
            });
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseSession();
        app.UseAuthentication();
        app.UseMiddleware<PasswordChangeEnforcementMiddleware>();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }

    private static X509Certificate2? LoadCertificate(CertificateSettings settings, string contentRoot)
    {
        if (!string.IsNullOrWhiteSpace(settings.Path))
        {
            var fullPath = Path.IsPathRooted(settings.Path)
                ? settings.Path
                : Path.Combine(contentRoot, settings.Path);

            if (File.Exists(fullPath))
            {
                return string.IsNullOrEmpty(settings.Password)
                    ? new X509Certificate2(fullPath)
                    : new X509Certificate2(fullPath, settings.Password);
            }
        }

        if (!string.IsNullOrWhiteSpace(settings.Thumbprint))
        {
            var storeName = Enum.TryParse(settings.StoreName, true, out StoreName parsedStoreName)
                ? parsedStoreName
                : StoreName.My;
            var storeLocation = Enum.TryParse(settings.StoreLocation, true, out StoreLocation parsedStoreLocation)
                ? parsedStoreLocation
                : StoreLocation.LocalMachine;

            using var store = new X509Store(storeName, storeLocation);
            store.Open(OpenFlags.ReadOnly);
            var matches = store.Certificates.Find(X509FindType.FindByThumbprint, settings.Thumbprint, validOnly: false);
            if (matches.Count > 0)
            {
                return matches[0];
            }
        }

        return null;
    }
}
