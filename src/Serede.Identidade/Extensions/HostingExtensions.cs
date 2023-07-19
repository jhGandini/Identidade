using IdentityServer4.AspNetIdentity;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Text.Json.Serialization;
using Serede.Identidade.Data;
using Serede.Identidade.Models;
using Serede.Identidade.Services;
using Serede.Identidade.Data.Repositories;

namespace Serede.Identidade.Extensions;


internal static class HostingExtensions
{
    public static WebApplication ConfigureServices(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("sqlConnection");

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString, dbOpts => dbOpts.MigrationsAssembly(typeof(Program).Assembly.FullName))
            );
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();
        //builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
        builder.Services.AddIdentity<SeredeUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = false)
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        var isBuilder = builder.Services
            .AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;

                // see https://docs.duendesoftware.com/identityserver/v5/fundamentals/resources/
                options.EmitStaticAudienceClaim = true;
            })
            .AddAspNetIdentity<SeredeUser>()
            .AddConfigurationStore(options =>
            {
                options.ConfigureDbContext = b =>
                    b.UseSqlServer(connectionString, dbOpts => dbOpts.MigrationsAssembly(typeof(Program).Assembly.FullName));
            })
            .AddOperationalStore(options =>
            {
                options.ConfigureDbContext = b =>
                    b.UseSqlServer(connectionString, dbOpts => dbOpts.MigrationsAssembly(typeof(Program).Assembly.FullName));

                options.EnableTokenCleanup = true;
                //options.RemoveConsumedTokens = true;
            })
            .AddProfileService<SeredeProfileService>()
            .AddDeveloperSigningCredential();


        builder.Services.AddScoped<EmailService>();

        builder.Services.AddScoped<IProfileService, SeredeProfileService>();


        builder.Services.AddCors(o => o.AddPolicy("CorsPolicy", builder =>
        {
            builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
                .WithExposedHeaders("*");
        }));

        //builder.Services.AddAuthorization(options =>
        //    options.AddPolicy("admin",
        //        policy => policy.RequireClaim("medicina.atestados.Acesso", "Sim"))
        ////policy => policy.RequireClaim("sub", "4b9530d5-20ec-497f-8d49-6c7d07ca83a0"))
        //);

        //builder.Services.Configure<RazorPagesOptions>(options =>
        //    options.Conventions.AuthorizeFolder("/Admin", "admin"));

        builder.Services.AddTransient<ClientRepository>();
        builder.Services.AddTransient<IdentityScopeRepository>();
        builder.Services.AddTransient<ApiScopeRepository>();

        builder.Services.AddControllers().AddJsonOptions(x =>
                x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

        builder.Services.AddRazorPages();

        //builder.Services.Configure<PasswordHasherOptions>(options => options.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2);

        return builder.Build();
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
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

        app.MapRazorPages();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapRazorPages().RequireAuthorization();
        });
        app.MapControllers();

        return app;
    }
}
