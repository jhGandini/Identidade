using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Validation;

namespace Identidade.Server.Extensions;

public class RedirectUriValidator : IRedirectUriValidator
{
    protected bool StringCollectionContainsString(IEnumerable<string> uris, string requestedUri)
    {
        if (uris.IsNullOrEmpty()) return false;

        return uris.Contains(requestedUri, StringComparer.OrdinalIgnoreCase);
    }


    public Task<bool> IsPostLogoutRedirectUriValidAsync(string requestedUri, Client client)
    {
        //if (!requestedUri.Equals("/"))
        //{
        //    var index = requestedUri.IndexOf("/", 8);
        //    requestedUri = requestedUri.Substring(0, index + 1);
        //}

        return Task.FromResult(StringCollectionContainsString(client.PostLogoutRedirectUris, requestedUri));
    }

    public Task<bool> IsRedirectUriValidAsync(string requestedUri, Client client)
    {
        if (!requestedUri.Equals("/"))
        {
            var index = requestedUri.IndexOf("/", 8);
            requestedUri = requestedUri.Substring(0, index + 1);
        }
        return Task.FromResult(StringCollectionContainsString(client.RedirectUris, requestedUri));
    }
}
