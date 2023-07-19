using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Test;
using Serede.Identidade.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;
using Serede.Identidade.Pages.Login;

namespace Serede.Identidade.Pages.Login
{
    //[SecurityHeaders]
    [AllowAnonymous]
    public class Index : PageModel
    {        
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;
        private readonly IAuthenticationSchemeProvider _schemeProvider;        
        private readonly SignInManager<SeredeUser> _signInManager;

        private readonly UserManager<SeredeUser> _userManager;

        public ViewModel View { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public Index(
            IIdentityServerInteractionService interaction,
            IAuthenticationSchemeProvider schemeProvider,            
            IEventService events
            ,SignInManager<SeredeUser> signInManager,
            UserManager<SeredeUser> userManager
            )
        {            
            _interaction = interaction;
            _schemeProvider = schemeProvider;         
            _events = events;

            _signInManager = signInManager;

            _userManager = userManager;


        }

        public async Task<IActionResult> OnGet(string returnUrl)
        {            
            await BuildModelAsync(returnUrl);

            Input.Password = "@Idp2023#";
            Input.Username = "IdentityAdmin";

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            // check if we are in the context of an authorization request
            var context = await _interaction.GetAuthorizationContextAsync(Input.ReturnUrl);

            // the user clicked the "cancel" button
            if (Input.Button != "login")
            {
                if (context != null)
                {
                    // if the user cancels, send a result back into IdentityServer as if they 
                    // denied the consent (even if this client does not require consent).
                    // this will send back an access denied OIDC error response to the client.
                    await _interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied);

                    // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                    if (context.IsNativeClient())
                    {
                        // The client is native, so this change in how to
                        // return the response is for better UX for the end user.
                        return this.LoadingPage(Input.ReturnUrl);
                    }

                    return Redirect(Input.ReturnUrl);
                }
                else
                {
                    // since we don't have a valid context, then we just go back to the home page
                    return Redirect("~/");
                }
            }

            if (ModelState.IsValid)
            {                
                var user = await _signInManager.UserManager.FindByNameAsync(Input.Username);

                //await _userManager.ResetAccessFailedCountAsync(user);
                //var checkPass = await _signInManager.PasswordSignInAsync(Input.Username, Input.Password, Input.RememberLogin, lockoutOnFailure: true);
                
                var checkPass = await _signInManager.CheckPasswordSignInAsync(user, Input.Password, true);

                    

                if (user != null && checkPass == SignInResult.Success && !user.IsExpired() && !user.Blocked)
                {                                            
                    await _events.RaiseAsync(new UserLoginSuccessEvent(user.UserName, user.Id, user.UserName, clientId: context?.Client.ClientId));

                    // only set explicit expiration here if user chooses "remember me". 
                    // otherwise we rely upon expiration configured in cookie middleware.
                    AuthenticationProperties props = null;
                    if (LoginOptions.AllowRememberLogin && Input.RememberLogin)
                    {
                        props = new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.Add(LoginOptions.RememberMeLoginDuration)
                        };
                    };

                    // issue authentication cookie with subject ID and username
                    var isuser = new IdentityServerUser(user.Id)
                    {
                        DisplayName = user.UserName,                            
                    };

                    var claims = await _signInManager.UserManager.GetClaimsAsync(user);
                    var identity = new ClaimsIdentity(claims, "pwd");
                    var userTst = new ClaimsPrincipal(identity);                                        

                    claims.ToList().ForEach(claim => isuser.AdditionalClaims.Add(claim));

                    await HttpContext.SignInAsync(isuser, props);

                    if (context != null)
                    {
                        if (context.IsNativeClient())
                        {
                            // The client is native, so this change in how to
                            // return the response is for better UX for the end user.
                            return this.LoadingPage(Input.ReturnUrl);
                        }

                        // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                        return Redirect(Input.ReturnUrl);
                    }

                    // request for a local page
                    if (Url.IsLocalUrl(Input.ReturnUrl))
                    {
                        return Redirect(Input.ReturnUrl);
                    }
                    else if (string.IsNullOrEmpty(Input.ReturnUrl))
                    {
                        return Redirect("~/");
                    }
                    else
                    {
                        // user might have clicked on a malicious link - should be logged
                        throw new Exception("invalid return URL");
                    }
                }               

                var message = "";
                switch (checkPass.ToString())
                {
                    case "Lockedout":
                        message = "Usuário bloqueado por muitas tentativas sem sucesso";
                        break;

                    case "Notallowed":
                        message = "Usuário não permitido";
                        break;
                    default:                        
                        if(user.IsExpired()) message = "Senha Expirada";
                        else if (user.Blocked) message = "Usuario bloqueado";
                        else message = "Usuário ou senha incorretos";
                        break;
                }


                await _events.RaiseAsync(new UserLoginFailureEvent(Input.Username, message, clientId: context?.Client.ClientId));
                ModelState.AddModelError(string.Empty, message);//LoginOptions.InvalidCredentialsErrorMessage
            }

            // something went wrong, show form with error
            await BuildModelAsync(Input.ReturnUrl);
            return Page();
        }

        private async Task BuildModelAsync(string returnUrl)
        {
            Input = new InputModel
            {
                ReturnUrl = returnUrl
            };

            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (context?.IdP != null && await _schemeProvider.GetSchemeAsync(context.IdP) != null)
            {
                var local = context.IdP == IdentityServer4.IdentityServerConstants.LocalIdentityProvider;

                // this is meant to short circuit the UI and only trigger the one external IdP
                View = new ViewModel
                {
                    EnableLocalLogin = local,
                };

                Input.Username = context?.LoginHint;

                if (!local)
                {
                    View.ExternalProviders = new[] { new ViewModel.ExternalProvider { AuthenticationScheme = context.IdP } };
                }

                return;
            }

            var schemes = await _schemeProvider.GetAllSchemesAsync();

            var providers = schemes
                .Where(x => x.DisplayName != null)
                .Select(x => new ViewModel.ExternalProvider
                {
                    DisplayName = x.DisplayName ?? x.Name,
                    AuthenticationScheme = x.Name
                }).ToList();
           

            var allowLocal = true;
            var client = context?.Client;
            if (client != null)
            {
                allowLocal = client.EnableLocalLogin;
                if (client.IdentityProviderRestrictions != null && client.IdentityProviderRestrictions.Any())
                {
                    providers = providers.Where(provider => client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme)).ToList();
                }
            }

            View = new ViewModel
            {
                AllowRememberLogin = LoginOptions.AllowRememberLogin,
                EnableLocalLogin = allowLocal && LoginOptions.AllowLocalLogin,
                ExternalProviders = providers.ToArray()
            };
        }
    }
}