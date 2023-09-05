using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Identidade.Server.Pages
{
    [Authorize]
    public class Index : PageModel
    {        
        private readonly IConfiguration _config;

        public Index(IConfiguration config)
        {            
            _config = config;
        }

        public void OnGet()
        {
            if (VerifyDefaultRedirect("/"))
                Redirect(_config.GetSection("ServerConfig:DefaultRedirectUrl").Value);                       
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