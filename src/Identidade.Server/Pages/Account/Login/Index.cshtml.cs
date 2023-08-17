using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using IdentityServer4.Test;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;
using Identidade.Server.Extensions;
using Identidade.Server.Models;
using IdentityServer4.Extensions;

namespace Identidade.Server.Pages.Account.Login
{
    //[SecurityHeaders]
    [AllowAnonymous]
    public class Index : PageModel
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;        
        private readonly SignInManager<SeredeUser> _signInManager;
        private readonly IConfiguration _config;

        private readonly UserManager<SeredeUser> _userManager;

        public ViewModel View { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public Index(
            IIdentityServerInteractionService interaction,            
            IEventService events
            , SignInManager<SeredeUser> signInManager,
            UserManager<SeredeUser> userManager,
            IConfiguration config
            )
        {
            _interaction = interaction;            
            _events = events;

            _signInManager = signInManager;

            _userManager = userManager;
            _config = config;
        }

        public async Task<IActionResult> OnGet(string returnUrl)
        {
            if (VerifyDefaultRedirect(returnUrl))
                return Redirect(_config.GetSection("ServerConfig:DefaultRedirectUrl").Value);

            BuildModelAsync(returnUrl);
            return Page();
        }

        public async Task<IActionResult> OnPost()
        {            
            var context = await _interaction.GetAuthorizationContextAsync(Input.ReturnUrl);
         
            if (Input.Button != "login")
            {
                if (context != null)
                {                    
                    await _interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied);
                 
                    if (context.IsNativeClient())
                    {                        
                        return this.LoadingPage(Input.ReturnUrl);
                    }

                    return Redirect(Input.ReturnUrl);
                }
                else
                {                    
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
                            return this.LoadingPage(Input.ReturnUrl);
                        }
                        return Redirect(Input.ReturnUrl);
                    }

                    if (Url.IsLocalUrl(Input.ReturnUrl))
                    {
                        if (VerifyDefaultRedirect(Input.ReturnUrl))
                            return Redirect(_config.GetSection("ServerConfig:DefaultRedirectUrl").Value);
                        else                        
                            return Redirect(Input.ReturnUrl);
                    }                           
                    else
                    {
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
                        if (user.IsExpired()) message = "Senha Expirada";
                        else if (user.Blocked) message = "Usuario bloqueado";
                        else message = "Usuário ou senha incorretos";
                        break;
                }


                await _events.RaiseAsync(new UserLoginFailureEvent(Input.Username, message, clientId: context?.Client.ClientId));
                ModelState.AddModelError(string.Empty, message);//LoginOptions.InvalidCredentialsErrorMessage
            }
            
            BuildModelAsync(Input.ReturnUrl);
            return Page();
        }

        private void BuildModelAsync(string returnUrl)
        {            
            Input = new InputModel
            {
                ReturnUrl = returnUrl,
                Password = "@Idp2023#",
                Username = "IdentityAdmin"
        };
            var allowLocal = true;                       

            View = new ViewModel
            {
                AllowRememberLogin = LoginOptions.AllowRememberLogin,
                EnableLocalLogin = allowLocal && LoginOptions.AllowLocalLogin,                
            };            
        }

        private bool VerifyDefaultRedirect(string redirect)
        {
            var redirectToDefault = false;
            bool.TryParse(_config.GetSection("ServerConfig:RedirectToDefault").Value, out redirectToDefault);

            if (redirect == "/" && redirectToDefault)
                return true;

            return false;
        }
    }
}