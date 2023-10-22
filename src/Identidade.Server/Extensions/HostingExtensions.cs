using IdentityServer4.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Text.Json.Serialization;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Identidade.Server.Data;
using Infra.Services.EmailService;
using Serede.Core.Extensions;
using IdentityServer4;

namespace Identidade.Server.Extensions;

internal static class HostingExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        builder.Services
        .AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo(@"C:\temp\dataprotection-persistkeys"))
        .AddKeyManagementOptions(options =>
        {
            options.NewKeyLifetime = new TimeSpan(365, 0, 0, 0);
            options.AutoGenerateKeys = true;
        });

        var email = new EmailSettings();
        builder.Configuration.Bind("Email", email);        

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("sqlConnection"), dbOpts => dbOpts.MigrationsAssembly(typeof(Program).Assembly.FullName))
            );
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();
        
        builder.Services.ConfgureIdentity();
        builder.ConfgureIdentityServer();
        builder.Services.ConfigureCors();

        builder.Services.AddAuthentication()
            .AddGoogle("Google", options =>
            {
                options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

                options.ClientId = "434483408261-55tc8n0cs4ff1fe21ea8df2o443v2iuc.apps.googleusercontent.com";//builder.Configuration["Authentication:Google:ClientId"];
                options.ClientSecret = "3gcoTrEDPPJ0ukn_aYYT6PWo";//builder.Configuration["Authentication:Google:ClientSecret"];
            });


        builder.Services.Configure<CookiePolicyOptions>(options =>
        {
            options.MinimumSameSitePolicy = SameSiteMode.Lax;
        });        

        builder.Services.AddSingleton<EmailSettings>(_ => email);
        builder.Services.AddScoped<EmailService>();
        builder.Services.AddScoped<IProfileService, SeredeProfile>();        

        builder.Services.AddControllers().AddJsonOptions(x =>
                x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

        builder.Services.AddRazorPages();

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseCookiePolicy();

        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseMigrationsEndPoint();
        }

        app.UseStaticFiles();
        app.UseCors("CorsPolicy");
        app.UseRouting();
        app.UseIdentityServer();
        app.UseAuthorization();
        app.UseAuthentication();
        app.MapRazorPages();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapRazorPages().RequireAuthorization();
        });
        app.MapControllers();

        return app;
    }

    public static X509Certificate2 LoadCertificate(this WebApplicationBuilder builder)
    {
        var key = new X509Certificate2(builder.Configuration.GetSection("Certificate:FileName").Value, builder.Configuration.GetSection("Certificate:Password").Value, X509KeyStorageFlags.MachineKeySet);
        return key;
    }
}
