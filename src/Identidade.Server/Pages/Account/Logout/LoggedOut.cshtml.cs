using IdentityServer4.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Identidade.Server.Models;

namespace Identidade.Server.Pages.Account.Logout
{
    [AllowAnonymous]
    public class LoggedOut : PageModel
    {
        //private readonly IIdentityServerInteractionService _interactionService;        

        //public LoggedOutViewModel View { get; set; }

        //public LoggedOut(IIdentityServerInteractionService interactionService)
        //{
        //    _interactionService = interactionService;
        //}

        //public async Task OnGet(string logoutId)
        //{
        //    // get context information (client name, post logout redirect URI and iframe for federated signout)
        //    var logout = await _interactionService.GetLogoutContextAsync(logoutId);

        //    View = new LoggedOutViewModel
        //    {
        //        AutomaticRedirectAfterSignOut = LogoutOptions.AutomaticRedirectAfterSignOut,
        //        PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
        //        ClientName = String.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout?.ClientName,
        //        SignOutIframeUrl = logout?.SignOutIFrameUrl
        //    };
        //}

        private readonly IIdentityServerInteractionService _interactionService;
        private readonly SignInManager<SeredeUser> _signInManager;
        private readonly ILogger<LoggedOut> _logger;

        public LoggedOutViewModel View { get; set; }

        public LoggedOut(IIdentityServerInteractionService interactionService, SignInManager<SeredeUser> signInManager, ILogger<LoggedOut> logger)
        {
            _interactionService = interactionService;
            _signInManager = signInManager;
            _logger = logger;
        }

        public async Task<IActionResult> OnGet(string logoutId)
        {
            var request = await _interactionService.GetLogoutContextAsync(logoutId);
            //if (request?.ShowSignoutPrompt == false || !User.Identity.IsAuthenticated)
            //{
            return await OnPost(logoutId);
            //}

            //return Page();
        }

        public bool _LoggedOut { get; set; }
        public string PostLogoutRedirectUri { get; set; }
        public string SignOutIframeUrl { get; set; }

        public async Task<IActionResult> OnPost(string logoutId)
        {
            _LoggedOut = true;

            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");

            var request = await _interactionService.GetLogoutContextAsync(logoutId);
            if (request != null)
            {
                PostLogoutRedirectUri = request.PostLogoutRedirectUri;
                SignOutIframeUrl = request.SignOutIFrameUrl;
            }

            View = new LoggedOutViewModel
            {
                AutomaticRedirectAfterSignOut = LogoutOptions.AutomaticRedirectAfterSignOut,
                //PostLogoutRedirectUri = string.IsNullOrEmpty(request?.PostLogoutRedirectUri) ? "/" : request?.PostLogoutRedirectUri,
                PostLogoutRedirectUri = request?.PostLogoutRedirectUri,
                ClientName = string.IsNullOrEmpty(request?.ClientName) ? request?.ClientId : request?.ClientName,
                SignOutIframeUrl = request?.SignOutIFrameUrl
            };

            return Page();
        }
    }
}