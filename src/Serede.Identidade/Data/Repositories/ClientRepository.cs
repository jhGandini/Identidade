using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Models;
using Microsoft.EntityFrameworkCore;
using Serede.Identidade.Models;
using Client = IdentityServer4.EntityFramework.Entities.Client;
using ClientClaim = IdentityServer4.EntityFramework.Entities.ClientClaim;

namespace Serede.Identidade.Data.Repositories;

public class ClientCadastro
{
    public int Id { get; set; }
    public string ClientId { get; set; }
    public string ClientName { get; set; }
    public string Flow { get; set; }
    public string Uris { get; set; }
    public bool AllowAccessTokensViaBrowser { get; set; }
    public bool Enabled { get; set; }
    public string Scopes { get; set; }
    public string LogoUri { get; set; }
    public bool AllowOfflineAccess { get; set; }
    public string Secret { get; set; }
    public bool DefaultScopeClaim { get; set; }
    public bool DefaultApiScope { get; set; }
}

public class ClientPermissao
{
    public int Id { get; set; }
    public string ClientId { get; set; }
    public string ClientName { get; set; }
    public bool Enabled { get; set; }
    public string Scopes { get; set; }
    public List<ClientClaim> Clains { get; set; }
}

public class ClientRepository
{
    public readonly ConfigurationDbContext _context;
    private readonly ApplicationDbContext _appContext;

    public ClientRepository(ConfigurationDbContext context, ApplicationDbContext appContext)
    {
        _context = context;
        _appContext = appContext;
    }

    public async Task<IEnumerable<Client>> GetAllScopesAsync(string filter = null)
    {
        var result = new List<ClientCadastro>();
        var grants = new[] { GrantType.AuthorizationCode, GrantType.ClientCredentials, GrantType.DeviceFlow, GrantType.Hybrid, GrantType.Implicit, GrantType.ResourceOwnerPassword };

        var query = _context.Clients
            .Include(x => x.AllowedScopes)
            .Include(x => x.Claims)
            .Where(x => x.AllowedGrantTypes.Any(grant => grants.Contains(grant.GrantType)));

        if (!String.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(x => x.ClientId.Contains(filter) || x.ClientName.Contains(filter));
        }



        return query.ToArray();
    }




    public async Task<IEnumerable<ClientCadastro>> GetAllAsync(string filter = null)
    {
        var result = new List<ClientCadastro>();
        var grants = new[] { GrantType.AuthorizationCode, GrantType.ClientCredentials, GrantType.DeviceFlow, GrantType.Hybrid, GrantType.Implicit, GrantType.ResourceOwnerPassword };

        var query = _context.Clients
            .Include(x => x.AllowedGrantTypes)
            .Where(x => x.AllowedGrantTypes.Any(grant => grants.Contains(grant.GrantType)));

        if (!String.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(x => x.ClientId.Contains(filter) || x.ClientName.Contains(filter));
        }

        foreach (var item in query)
        {
            result.Add(
                new ClientCadastro
                {
                    Id = item.Id,
                    ClientId = item.ClientId,
                    ClientName = item.ClientName,
                    Flow = item.AllowedGrantTypes.Any() ? item.AllowedGrantTypes.Select(x => x.GrantType).Aggregate((a, b) => $"{a} {b}") : null,
                    Enabled = item.Enabled
                });
        }

        return result.ToArray();
    }

    public async Task<ClientCadastro> GetByIdAsync(string id)
    {
        var client = await _context.Clients
            .Include(x => x.AllowedGrantTypes)
            .Include(x => x.AllowedScopes)
            .Include(x => x.RedirectUris)
            .Include(x => x.PostLogoutRedirectUris)
            .Where(x => x.ClientId == id)
            .SingleOrDefaultAsync();

        if (client == null) return null;

        return new ClientCadastro
        {
            Id = client.Id,
            ClientId = client.ClientId,
            ClientName = client.ClientName,
            Flow = client.AllowedGrantTypes.Any() ? client.AllowedGrantTypes.Select(x => x.GrantType).Aggregate((a, b) => $"{a} {b}") : null,
            Scopes = client.AllowedScopes.Any() ? client.AllowedScopes.Select(x => x.Scope).Aggregate((a, b) => $"{a} {b}") : null,
            Uris = client.RedirectUris.Any() ? client.RedirectUris.Select(x => x.RedirectUri).Aggregate((a, b) => $"{a} {b}") : null,
            AllowOfflineAccess = client.AllowOfflineAccess,
            AllowAccessTokensViaBrowser = client.AllowAccessTokensViaBrowser,
            Enabled = client.Enabled,
        };
    }

    public async Task<List<ClientPermissao>> GetAllFullJoinAsync()
    {
        var ret = new List<ClientPermissao>();

        var clients = await _context.Clients
            .Include(x => x.AllowedScopes)
            .Include(x => x.Claims)
            .Where(x => x.Enabled.Equals(true))
            .ToListAsync();

        foreach (var client in clients)
        {
            ret.Add(
                new ClientPermissao
                {
                    Id = client.Id,
                    ClientId = client.ClientId,
                    ClientName = client.ClientName,
                    Scopes = client.AllowedScopes.Any() ? client.AllowedScopes.Select(x => x.Scope).Aggregate((a, b) => $"{a} {b}") : null,
                    Clains = client.Claims.Any() ? client.Claims : null,
                    Enabled = client.Enabled,
                });
        }

        return ret;
    }


    public async Task CreateAsync(ClientCadastro model)
    {
        var client = new IdentityServer4.Models.Client();
        client.ClientId = model.ClientId.Trim();
        client.ClientName = model.ClientName?.Trim();

        client.ClientSecrets.Add(new IdentityServer4.Models.Secret(model.Secret.Sha256()));


        if (model.Flow == "ClientCredentials")
        {
            client.AllowedGrantTypes = GrantTypes.ClientCredentials;
        }
        else
        {
            client.AllowedGrantTypes = GrantTypes.Code;
            client.AllowOfflineAccess = true;
        }

        _context.Clients.Add(client.ToEntity());
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ClientCadastro model)
    {
        var client = await _context.Clients
            .Include(x => x.AllowedGrantTypes)
            .Include(x => x.AllowedScopes)
            .Include(x => x.RedirectUris)
            .Include(x => x.PostLogoutRedirectUris)
            .SingleOrDefaultAsync(x => x.ClientId == model.ClientId);

        if (client == null) throw new Exception("Invalid Client Id");

        if (client.ClientName != model.ClientName)
        {
            client.ClientName = model.ClientName?.Trim();
        }

        var scopes = model.Scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToArray();
        var currentScopes = (client.AllowedScopes.Select(x => x.Scope) ?? Enumerable.Empty<String>()).ToArray();

        var scopesToAdd = scopes.Except(currentScopes).ToArray();
        var scopesToRemove = currentScopes.Except(scopes).ToArray();

        if (scopesToRemove.Any())
        {
            client.AllowedScopes.RemoveAll(x => scopesToRemove.Contains(x.Scope));
        }
        if (scopesToAdd.Any())
        {
            client.AllowedScopes.AddRange(scopesToAdd.Select(x => new ClientScope
            {
                Scope = x,
            }));
        }

        //var flow = client.AllowedGrantTypes.Select(x => x.GrantType)
        //    .Single() == GrantType.ClientCredentials ? Flow.ClientCredentials : Flow.CodeFlowWithPkce;

        //if (flow == Flow.CodeFlowWithPkce)
        //{
        //    if (client.RedirectUris.SingleOrDefault()?.RedirectUri != model.RedirectUri)
        //    {
        //        client.RedirectUris.Clear();
        //        if (model.RedirectUri != null)
        //        {
        //            client.RedirectUris.Add(new ClientRedirectUri { RedirectUri = model.RedirectUri.Trim() });
        //        }
        //    }
        //    if (client.PostLogoutRedirectUris.SingleOrDefault()?.PostLogoutRedirectUri != model.PostLogoutRedirectUri)
        //    {
        //        client.PostLogoutRedirectUris.Clear();
        //        if (model.PostLogoutRedirectUri != null)
        //        {
        //            client.PostLogoutRedirectUris.Add(new ClientPostLogoutRedirectUri { PostLogoutRedirectUri = model.PostLogoutRedirectUri.Trim() });
        //        }
        //    }
        //    if (client.FrontChannelLogoutUri != model.FrontChannelLogoutUri)
        //    {
        //        client.FrontChannelLogoutUri = model.FrontChannelLogoutUri?.Trim();
        //    }
        //    if (client.BackChannelLogoutUri != model.BackChannelLogoutUri)
        //    {
        //        client.BackChannelLogoutUri = model.BackChannelLogoutUri?.Trim();
        //    }
        //}

        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string clientId)
    {
        var client = await _context.Clients.SingleOrDefaultAsync(x => x.ClientId == clientId);

        if (client == null) throw new Exception("Invalid Client Id");

        _context.Clients.Remove(client);
        await _context.SaveChangesAsync();
    }


    public async Task<List<SeredeClient>> GetClientsDefaultClain()
    {
        var clients = await _appContext.SeredeClient.Include(x=>x.MngClaims).ToListAsync();
        return clients;
    }

}
