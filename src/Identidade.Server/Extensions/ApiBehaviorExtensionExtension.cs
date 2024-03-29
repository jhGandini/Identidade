﻿using Microsoft.AspNetCore.Mvc;

namespace Identidade.Server.Extensions;


public static class ApiBehaviorExtensionExtension
{
    public static void SuppressModelStateInvalid(this IServiceCollection services, bool val = true)
    {
        services.Configure<ApiBehaviorOptions>(opt =>
        {
            opt.SuppressModelStateInvalidFilter = val;
        });
    }
}
