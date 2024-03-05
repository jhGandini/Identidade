using Duende.IdentityServer.AspNetIdentity;
using Duende.IdentityServer.Models;
using Identidade.Server.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace Identidade.Server.Extensions;


public class SeredeProfile : ProfileService<SeredeUser>
{

    protected new readonly IUserClaimsPrincipalFactory<SeredeUser> ClaimsFactory;

    /// <summary>
    /// The logger
    /// </summary>
    protected new readonly ILogger<ProfileService<SeredeUser>> Logger;

    /// <summary>
    /// The user manager.
    /// </summary>
    protected new readonly UserManager<SeredeUser> UserManager;

    public SeredeProfile(UserManager<SeredeUser> userManager, IUserClaimsPrincipalFactory<SeredeUser> claimsFactory) : base(userManager, claimsFactory)
    {
        UserManager = userManager;
        ClaimsFactory = claimsFactory;
    }

    protected override async Task GetProfileDataAsync(ProfileDataRequestContext context, SeredeUser user)
    {
        var principal = await GetUserClaimsAsync(user);


        if (principal.Identity != null)
        {
            ((ClaimsIdentity)principal.Identity).AddClaims(
                new[] {
                    new Claim("cpf", user.CPF ?? string.Empty),
                    new Claim("first_name", user.FirstName ?? string.Empty),
                    new Claim("last_name", user.LastName ?? string.Empty)
                });
        }

        context.AddRequestedClaims(principal.Claims);

    }
}


