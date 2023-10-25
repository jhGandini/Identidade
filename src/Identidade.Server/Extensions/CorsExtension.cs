using Identidade.Server.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Identidade.Server.Extensions;

public static class CorsExtension
{
    public static void ConfigureCors(this IServiceCollection services, string corsName = "CorsPolicy", CorsSettings corsSettings = null)
    {
        if (corsSettings == null)
        {
            services.AddCors(o => o.AddPolicy(corsName, builder =>
            {
                builder
                    .AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .WithExposedHeaders("*");
            }));
        }
        else
        {
            services.AddCors(o => o.AddPolicy(corsName, builder =>
            {
                builder
                    .WithOrigins(corsSettings.Origins)
                    .WithMethods(corsSettings.Methods)
                    .WithHeaders(corsSettings.Headers)
                    .WithExposedHeaders(corsSettings.ExposedHeaders);
            }));
        }
    }

    public static CorsSettings BindCorsSettings(WebApplicationBuilder builder, string appSettingsKey = "CorsSettings")
    {
        var cors = new CorsSettings();
        builder.Configuration.Bind("CorsSettings", cors);
        builder.Services.AddSingleton(cors);

        return cors;
    }

}
