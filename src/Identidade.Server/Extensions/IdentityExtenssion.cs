using Identidade.Server.Data;
using Identidade.Server.Models;
using Microsoft.AspNetCore.Identity;

namespace Identidade.Server.Extensions;

public static class IdentityExtenssion
{
    public static IServiceCollection ConfgureIdentity(this IServiceCollection builder)
    {
        builder.AddIdentity<SeredeUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = false)
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        return builder;
    }
}
