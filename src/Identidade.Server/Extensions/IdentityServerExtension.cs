using Identidade.Server.Data;
using Identidade.Server.Models;
using IdentityServer4;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Identidade.Server.Extensions;

public static class IdentityServerExtension
{
    public static void ConfgureIdentityServer(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("sqlConnection");

        builder.Services.AddIdentityServer(options =>
        {
            options.Events.RaiseErrorEvents = true;
            options.Events.RaiseInformationEvents = true;
            options.Events.RaiseFailureEvents = true;
            options.Events.RaiseSuccessEvents = true;
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
            .AddProfileService<SeredeProfile>()
            .AddSigningCredential(builder.LoadCertificate())
            .AddValidationKey(builder.LoadCertificate())
            .AddRedirectUriValidator<RedirectUriValidator>();
    }
}
