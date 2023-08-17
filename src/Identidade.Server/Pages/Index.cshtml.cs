using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Reflection;

namespace Identidade.Server.Pages
{
    [Authorize]
    public class Index : PageModel
    {
        public string Version;

        public void OnGet()
        {
            Version = typeof(IdentityServer4.Hosting.IdentityServerMiddleware).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+').First();
        }
    }
}