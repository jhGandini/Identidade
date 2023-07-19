using IdentityServer4.AspNetIdentity;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Identity;
using Serede.Identidade.Models;
using System.Security.Claims;

namespace Serede.Identidade.Services;


public class SeredeProfileService : ProfileService<SeredeUser>
{

    protected readonly IUserClaimsPrincipalFactory<SeredeUser> ClaimsFactory;

    /// <summary>
    /// The logger
    /// </summary>
    protected readonly ILogger<ProfileService<SeredeUser>> Logger;

    /// <summary>
    /// The user manager.
    /// </summary>
    protected readonly UserManager<SeredeUser> UserManager;

    public SeredeProfileService(UserManager<SeredeUser> userManager, IUserClaimsPrincipalFactory<SeredeUser> claimsFactory) : base(userManager, claimsFactory)
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


