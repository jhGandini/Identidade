using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.EntityFramework.Entities;
using IdentityServer4.EntityFramework.Mappers;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Stores;
using Serede.Identidade.Data;
using Serede.Identidade.Data.Repositories;
using Serede.Identidade.Models;
using IdentityModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Claims;
using static System.Formats.Asn1.AsnWriter;
using static System.Reflection.Metadata.BlobBuilder;

namespace Serede.Identidade.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ScopeController : ControllerBase
{

    private readonly ApiScopeRepository _apiScopeRepository;
    private readonly ILogger<ScopeController> _logger;
    private readonly IClientStore _clients;
    private readonly IResourceStore _resources;
    private readonly ApplicationDbContext _applicationDbContext;
    private readonly ConfigurationDbContext _configurationDbContext;
    public ScopeController(ApiScopeRepository apiScopeRepository, ILogger<ScopeController> logger, IClientStore clients, IResourceStore resources, ApplicationDbContext applicationDbContext, ConfigurationDbContext configurationDbContext)
    {
        _logger = logger;
        _apiScopeRepository = apiScopeRepository;
        _clients = clients;
        _resources = resources;
        _applicationDbContext = applicationDbContext;
        _configurationDbContext = configurationDbContext;
    }

    [AllowAnonymous]
    [HttpGet("StringList")]
    public async Task<ActionResult> GetStringList()
    {
        try
        {                                   
            var a = await _resources.GetAllEnabledResourcesAsync();
            //a.ToScopeNames()
            var x = Enumerable.Union(a.ApiScopes.Select(x=>x.Name).ToList(), a.IdentityResources.Select(x => x.Name).ToList()).ToList();

            x = x.OrderBy(q => q).ToList();

            return Ok(new ResultViewModel(x));
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
    [HttpGet()]
    public async Task<ActionResult> GetList([FromQuery] UsuarioSelector val)
    {
        try
        {
            var queryApi = _configurationDbContext.ApiScopes                
                .Include(x => x.UserClaims)
                .AsQueryable();

            var queryIdentity = _configurationDbContext.IdentityResources
                .Include(x => x.UserClaims)                
                .AsQueryable();
            
            var x = Enumerable.Union(
                queryApi.Select(x => 
                new
                {x.Id, x.Enabled,x.Name,x.DisplayName,x.Description,Type = "Resource"}).ToList(),
                queryIdentity.Select(x =>
                new
                { x.Id, x.Enabled,x.Name,x.DisplayName,x.Description,Type = "Identity"}).ToList()).ToList();

            x = x.OrderBy(q => q.Name).ToList();

            return Ok(new ResultViewModel(x));
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
    [HttpGet("{id}/{name}")]
    public async Task<ActionResult> Get(int id, string name)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ResultViewModel(id, ModelState));

            var a = _configurationDbContext.ApiScopes.Include(x => x.UserClaims).FirstOrDefault(x => x.Id == id && x.Name == name);
            if (a != null)
            {
                return Ok(new ResultViewModel(new {a.Id, a.Enabled, a.Name, a.DisplayName, a.Description, Type = "Resource", UserClaims = a.UserClaims.Select( x => x.Type).ToArray() }));
            }
            var b = _configurationDbContext.IdentityResources.Include(x => x.UserClaims).FirstOrDefault(x => x.Id == id && x.Name == name);
            if (b != null)
            {
                return Ok(new ResultViewModel(new { b.Id, b.Enabled, b.Name, b.DisplayName, b.Description, Type = "Resource", UserClaims = b.UserClaims.Select(x => x.Type).ToArray() }));
            }

            return Ok(new ResultViewModel());
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

    private async Task<int> TratarIdentity(ResourceCadastro model)
    {        
        var scope = await _configurationDbContext.IdentityResources.Include(x => x.UserClaims).FirstOrDefaultAsync(x => x.Id.Equals(model.Id)); 
        var claims = model.Claims.Split(" ").ToList();
        if (scope != null)
        {
            
            var currentClaims = (scope.UserClaims.Select(x => x.Type) ?? Enumerable.Empty<String>()).ToArray();
            var claimsToAdd = claims.Except(currentClaims).ToArray();
            var claimsToRemove = currentClaims.Except(claims).ToArray();
            if (claimsToRemove.Any())
            {
                foreach (var item in claimsToRemove)
                {
                    model.UserClaims.Remove(item);
                }
            }
            if (claimsToAdd.Any())
            {
                foreach (var item in claimsToAdd)
                {
                    scope.UserClaims.Add(new IdentityResourceClaim() { Type = item });
                    model.UserClaims.Remove(item);
                }
            }
        }
        else
        {
            scope = new IdentityServer4.EntityFramework.Entities.IdentityResource();
            scope.UserClaims = new List<IdentityResourceClaim>();
            foreach (var item in claims)
            {
                scope.UserClaims.Add(new IdentityResourceClaim() { Type = item });
            }
        }

        scope.Description = model.Description;
        scope.Name = model.Name;
        scope.DisplayName = model.DisplayName;
        scope.Enabled = model.Enabled;

        if (scope.Id != 0)
            _configurationDbContext.IdentityResources.Update(scope);
        else
            _configurationDbContext.IdentityResources.Add(scope);
        
        return await _configurationDbContext.SaveChangesAsync();
    }

    private async Task<int> TratarApi(ResourceCadastro model)
    {
        var scope = await _configurationDbContext.ApiScopes.Include(x => x.UserClaims).FirstOrDefaultAsync(x => x.Id.Equals(model.Id));        
        var claims = model.Claims.Split(" ").ToList();
        if (scope != null)
        {            
            
                       
            var currentClaims = (scope.UserClaims.Select(x => x.Type) ?? Enumerable.Empty<String>()).ToArray();
            var claimsToAdd = claims.Except(currentClaims).ToArray();
            var claimsToRemove = currentClaims.Except(claims).ToArray();
            if (claimsToRemove.Any())
            {
                foreach (var item in claimsToRemove)
                {
                    model.UserClaims.Remove(item);
                }                
            }
            if (claimsToAdd.Any())
            {
                foreach (var item in claimsToAdd)
                {
                    scope.UserClaims.Add(new ApiScopeClaim() { Type = item });
                    model.UserClaims.Remove(item);
                }                
            }
        }
        else
        {
            scope = new IdentityServer4.EntityFramework.Entities.ApiScope();
            scope.UserClaims = new List<ApiScopeClaim>();
            foreach (var item in claims)
            {
                scope.UserClaims.Add(new ApiScopeClaim() { Type = item });
            }
        }

        scope.Description = model.Description;
        scope.Name = model.Name;
        scope.DisplayName = model.DisplayName;
        scope.Enabled = model.Enabled;

        if(scope.Id != 0)
            _configurationDbContext.ApiScopes.Update(scope);
        else
            _configurationDbContext.ApiScopes.Add(scope);

        return await _configurationDbContext.SaveChangesAsync();
    }

    [AllowAnonymous]
    [HttpPost]
    public async Task<ActionResult> Post(ResourceCadastro model)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ResultViewModel(model, ModelState));

            var result = 0;

            if (model.Type == "Identity")
                result = await TratarIdentity(model);
            else
                result = await TratarApi(model);

            return Ok(new ResultViewModel(result));

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