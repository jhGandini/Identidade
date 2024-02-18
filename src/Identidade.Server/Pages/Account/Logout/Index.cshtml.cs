using Identidade.Server.Models;
using IdentityModel;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Identidade.Server.Pages.Account.Logout
{
    //[SecurityHeaders]
    [AllowAnonymous]
    public class Index : PageModel
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;
        private readonly SignInManager<SeredeUser> _signInManager;

        [BindProperty]
        public string LogoutId { get; set; }
        [BindProperty]
        public string Button { get; set; }

        public Index(IIdentityServerInteractionService interaction, IEventService events, SignInManager<SeredeUser> signInManager)
        {
            _interaction = interaction;
            _events = events;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> OnGet(string logoutId)
        {
            LogoutId = logoutId;

            var showLogoutPrompt = LogoutOptions.ShowLogoutPrompt;

            if (User?.Identity.IsAuthenticated != true)
            {
                // if the user is not authenticated, then just show logged out page
                showLogoutPrompt = false;
            }
            else
            {
                var context = await _interaction.GetLogoutContextAsync(LogoutId);
                if (context?.ShowSignoutPrompt == false)
                {
                    // it's safe to automatically sign-out
                    showLogoutPrompt = false;
                }
            }

            if (showLogoutPrompt == false)
            {
                // if the request for logout was properly authenticated from IdentityServer, then
                // we don't need to show the prompt and can just log the user out directly.
                return await OnPost();
            }

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            if (Button == "nao")
            {
                return Redirect("~/");
            }

            var logout = await _interaction.GetLogoutContextAsync(LogoutId);

            var vm = new LoggedOutViewModel
            {
                AutomaticRedirectAfterSignOut = LogoutOptions.AutomaticRedirectAfterSignOut,
                PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
                ClientName = string.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout?.ClientName,
                SignOutIframeUrl = logout?.SignOutIFrameUrl,
                LogoutId = LogoutId
            };

            if (User?.Identity.IsAuthenticated == true)
            {
                var idp = User.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
                if (idp != null && idp != IdentityServer4.IdentityServerConstants.LocalIdentityProvider)
                {
                    var providerSupportsSignout = await HttpContext.GetSchemeSupportsSignOutAsync(idp);
                    if (providerSupportsSignout)
                    {

                        vm.LogoutId ??= await _interaction.CreateLogoutContextAsync();

                        vm.ExternalAuthenticationScheme = idp;
                    }
                }
            }

            if (User?.Identity.IsAuthenticated == true)
            {
                //// if there's no current logout context, we need to create one
                //// this captures necessary info from the current logged in user
                //// this can still return null if there is no context needed
                //LogoutId ??= await _interaction.CreateLogoutContextAsync();

                // delete local authentication cookie
                await _signInManager.SignOutAsync();

                // raise the logout event
                await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));

                // see if we need to trigger federated logout
                //var idp = User.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
            }
            // if it's a local login we can ignore this workflow
            if (vm.TriggerExternalSignout)
            {
                // build a return URL so the upstream provider will redirect back
                // to us after the user has logged out. this allows us to then
                // complete our single sign-out processing.
                string url = Url.Page("/Account/Logout/Loggedout", new { logoutId = LogoutId });

                // this triggers a redirect to the external provider for sign-out
                return SignOut(new AuthenticationProperties { RedirectUri = url }, vm.ExternalAuthenticationScheme);
            }



            return RedirectToPage("/Account/Logout/LoggedOut", new { logoutId = LogoutId });
        }
    }
}