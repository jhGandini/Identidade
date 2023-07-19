using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Serede.Identidade.Data.Repositories;
using Serede.Identidade.Models;
using IdentityModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Net;
using Client = IdentityServer4.EntityFramework.Entities.Client;
using Secret = IdentityServer4.EntityFramework.Entities.Secret;
using Serede.Identidade.Data;
using System.Linq;
using static System.Formats.Asn1.AsnWriter;
using System.Security.Claims;
using IdentityServer4.EntityFramework.DbContexts;

namespace Serede.Identidade.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClientController : ControllerBase
{

    private readonly ClientRepository _clientRepository;
    private readonly ILogger<ClientController> _logger;
    private readonly UserManager<SeredeUser> _userManager;
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly ConfigurationDbContext _configurationDbContext;
    public ClientController(ClientRepository clientRepository, ILogger<ClientController> logger, UserManager<SeredeUser> userManager, ApplicationDbContext applicationDbContext, ConfigurationDbContext configurationDbContext)
    {
        _logger = logger;
        _clientRepository = clientRepository;
        _userManager = userManager;
        _applicationDbContext = applicationDbContext;
        _configurationDbContext = configurationDbContext;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult> GetList([FromQuery] UsuarioSelector val)
    {
        try
        {            
            if (!ModelState.IsValid) return BadRequest(new ResultViewModel(val, ModelState));
            return Ok(new ResultViewModel(await _clientRepository.GetAllAsync(val.Busca)));
        }
        catch (HttpRequestException ex)
        {
            var er = new ResultViewModel();

            if (ex.StatusCode == HttpStatusCode.Unauthorized)
                er.AddNotification("Erro", "Voce não está autenticado ou não possui acesso a essa funcionalidade");
            else
                er.AddNotification("Erro", ex.Message);

            return BadRequest(er);
        }
        catch (Exception ex)
        {
            var er = new ResultViewModel();
            er.AddNotification("Erro", ex.Message);
            return BadRequest(er);
        }
    }

    [AllowAnonymous]
    [HttpGet("ListScopes")]
    public async Task<ActionResult> GetListScopes()
    {
        try
        {            
            return Ok(new ResultViewModel(await _clientRepository.GetAllScopesAsync()));
        }
        catch (HttpRequestException ex)
        {
            var er = new ResultViewModel();

            if (ex.StatusCode == HttpStatusCode.Unauthorized)
                er.AddNotification("Erro", "Voce não está autenticado ou não possui acesso a essa funcionalidade");
            else
                er.AddNotification("Erro", ex.Message);

            return BadRequest(er);
        }
        catch (Exception ex)
        {
            var er = new ResultViewModel();
            er.AddNotification("Erro", ex.Message);
            return BadRequest(er);
        }
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<ActionResult> Get(string id)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ResultViewModel(id, ModelState));
            return Ok(new ResultViewModel(await _clientRepository.GetByIdAsync(id)));
        }
        catch (HttpRequestException ex)
        {
            var er = new ResultViewModel();

            if (ex.StatusCode == HttpStatusCode.Unauthorized)
                er.AddNotification("Erro", "Voce não está autenticado ou não possui acesso a essa funcionalidade");
            else
                er.AddNotification("Erro", ex.Message);

            return BadRequest(er);
        }
        catch (Exception ex)
        {
            var er = new ResultViewModel();
            er.AddNotification("Erro", ex.Message);
            return BadRequest(er);
        }
    }

    [AllowAnonymous]
    [HttpGet("FullJoin")]
    public async Task<ActionResult> GetFullJoin()
    {
        try
        {            
            //return Ok(new ResultViewModel(await _clientRepository.GetAllFullJoinAsync()));
            return Ok(new ResultViewModel(await _clientRepository.GetClientsDefaultClain()));
        }
        catch (HttpRequestException ex)
        {
            var er = new ResultViewModel();

            if (ex.StatusCode == HttpStatusCode.Unauthorized)
                er.AddNotification("Erro", "Voce não está autenticado ou não possui acesso a essa funcionalidade");
            else
                er.AddNotification("Erro", ex.Message);

            return BadRequest(er);
        }
        catch (Exception ex)
        {
            var er = new ResultViewModel();
            er.AddNotification("Erro", ex.Message);
            return BadRequest(er);
        }
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult> Post(ClientCadastro model)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ResultViewModel(model, ModelState));
            
            var client = new Client();
            
            if (model.Id != 0) 
            { 
                client = await _clientRepository._context.Clients
               .Include(x => x.AllowedGrantTypes)
               .Include(x => x.AllowedScopes)
               .Include(x => x.RedirectUris)
               .Include(x => x.PostLogoutRedirectUris)
               .Include(x => x.AllowedCorsOrigins)
               .SingleOrDefaultAsync(x => x.Id == model.Id);
            }
            else
            {
                client.AllowedGrantTypes = new List<ClientGrantType>();
                client.AllowedScopes = new List<ClientScope>();
                client.RedirectUris = new List<ClientRedirectUri>();
                client.AllowedCorsOrigins = new List<ClientCorsOrigin>();
                client.PostLogoutRedirectUris = new List<ClientPostLogoutRedirectUri>();
                client.ClientSecrets = new List<ClientSecret>();
            }


            ResultViewModel resultModel;
            
            client.ClientId = model.ClientId.Trim();
            client.ClientName = model.ClientName.Trim();
            client.RequireConsent = false;

            var flows = model.Flow.Split(" ").ToList();
            var currentGrants = (client.AllowedGrantTypes.Select(x => x.GrantType) ?? Enumerable.Empty<String>()).ToArray();
            var grantsToAdd = flows.Except(currentGrants).ToArray();
            var grantsToRemove = currentGrants.Except(flows).ToArray();
            if (grantsToRemove.Any())
            {
                client.AllowedGrantTypes.RemoveAll(x => grantsToRemove.Contains(x.GrantType));
            }
            if (grantsToAdd.Any())
            {
                client.AllowedGrantTypes.AddRange(grantsToAdd.Select(x => new ClientGrantType
                {
                    GrantType = x,
                }));
            }

            var scopes = model.Scopes.Split(" ").ToList();
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

            var uris = model.Uris.Split(" ").ToList();
            var currentUris = (client.RedirectUris.Select(x => x.RedirectUri) ?? Enumerable.Empty<String>()).ToArray();
            var urisToAdd = uris.Except(currentUris).ToArray();
            var urisToRemove = currentUris.Except(uris).ToArray();
            if (urisToRemove.Any())
            {
                client.RedirectUris.RemoveAll(x => urisToRemove.Contains(x.RedirectUri));
                client.AllowedCorsOrigins.RemoveAll(x => urisToRemove.Contains(x.Origin));
                client.PostLogoutRedirectUris.RemoveAll(x => urisToRemove.Contains(x.PostLogoutRedirectUri));
            }
            if (urisToAdd.Any())
            {
                client.RedirectUris.AddRange(urisToAdd.Select(x => new ClientRedirectUri { RedirectUri = x}));
                client.AllowedCorsOrigins.AddRange(urisToAdd.Select(x => new ClientCorsOrigin { Origin = x }));
                client.PostLogoutRedirectUris.AddRange(urisToAdd.Select(x => new ClientPostLogoutRedirectUri { PostLogoutRedirectUri = x }));
            }

            client.AllowAccessTokensViaBrowser = model.AllowAccessTokensViaBrowser;
            client.AllowOfflineAccess = model.AllowOfflineAccess;            
            
            if(!string.IsNullOrEmpty(model.Secret))
                client.ClientSecrets.Add(new ClientSecret() { Value = model.Secret.Sha256()});
            
            if (!string.IsNullOrEmpty(model.LogoUri))
                client.LogoUri = model.LogoUri;

            if (model.Id == 0)
            {
                if (model.DefaultApiScope)
                {
                    var apiScope = new IdentityServer4.EntityFramework.Entities.ApiScope()
                    {
                        Description = "Default Api Scope",
                        Name = client.ClientId + ".Api",
                        DisplayName = client.ClientId + ".Api",
                        Enabled = true,
                    };

                    _configurationDbContext.ApiScopes.Add(apiScope);

                    client.AllowedScopes.Add(new ClientScope
                    {
                        Scope = apiScope.Name,
                    });
                }


                if (model.DefaultScopeClaim)
                {
                    
                    var scope = new IdentityServer4.EntityFramework.Entities.IdentityResource();
                    scope.UserClaims = new List<IdentityResourceClaim>();
                    scope.UserClaims.Add(new IdentityResourceClaim() { Type = client.ClientId + ".Acesso" });
                    scope.UserClaims.Add(new IdentityResourceClaim() { Type = client.ClientId + ".Perfil" });

                    scope.Description = "Default Scope";
                    scope.Name = client.ClientId;
                    scope.DisplayName = client.ClientId;
                    scope.Enabled = true;


                    client.AllowedScopes.Add(new ClientScope
                    {
                        Scope = scope.Name,
                    });



                    _configurationDbContext.IdentityResources.Add(scope);
                }
                
                _clientRepository._context.Clients.Add(client);                
            }
           
            var result = await _clientRepository._context.SaveChangesAsync();


            if (model.DefaultScopeClaim && result > 0)
            {
                await _applicationDbContext.MngClaims.AddAsync(new MngClaim()
                {
                    Name = client.ClientId + ".Acesso",
                    Type = "Lista",
                    Value = "Sim,Não",
                    DefaultValue = "Não",
                    ClientId = client.Id,
                    Enabled = true,
                });
                await _applicationDbContext.MngClaims.AddAsync(new MngClaim()
                {
                    Name = client.ClientId + ".Perfil",
                    Type = "Lista",
                    Value = "adm,colab",
                    DefaultValue = "colab",
                    ClientId = client.Id,
                    Enabled = true,
                });
                await _applicationDbContext.SaveChangesAsync();
            }

            return Ok(new ResultViewModel(client));

        }
        catch (HttpRequestException ex)
        {
            var er = new ResultViewModel();

            if (ex.StatusCode == HttpStatusCode.Unauthorized)
                er.AddNotification("Erro", "Voce não está autenticado ou não possui acesso a essa funcionalidade");
            else
                er.AddNotification("Erro", ex.Message);

            return BadRequest(er);
        }
        catch (Exception ex)
        {
            var er = new ResultViewModel();
            er.AddNotification("Erro", ex.Message);
            return BadRequest(er);
        }
    }

    [AllowAnonymous]
    [HttpGet("ClientsPermitidos/{userId}")]
    public async Task<ActionResult> GetClientsPermitidos(string userId)
    {
        var listNaoExibir = new List<string>() { "silent-refresh", "signin-oidc" };


        try
        {
            if (!ModelState.IsValid) return BadRequest(new ResultViewModel(userId, ModelState));


            var user = await _userManager.FindByNameAsync(userId);
            var claims = await _userManager.GetClaimsAsync(user);

            var permitidas = claims.Where(x=> x.Type.Contains("Acesso") && x.Value.Equals("Sim")).Select(x=>x.Type).ToList();

            
            var clients = await _applicationDbContext.MngClaims
                                    .Include(x => x.Client)
                                    .ThenInclude(x => x.Uris)                                                                        
                                    .Where(x => permitidas.Contains(x.Name)).ToListAsync();

            return Ok(new ResultViewModel(clients));
        }
        catch (HttpRequestException ex)
        {
            var er = new ResultViewModel();

            if (ex.StatusCode == HttpStatusCode.Unauthorized)
                er.AddNotification("Erro", "Voce não está autenticado ou não possui acesso a essa funcionalidade");
            else
                er.AddNotification("Erro", ex.Message);

            return BadRequest(er);
        }
        catch (Exception ex)
        {
            var er = new ResultViewModel();
            er.AddNotification("Erro", ex.Message);
            return BadRequest(er);
        }
    }
}