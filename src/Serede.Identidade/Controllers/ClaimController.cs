using IdentityServer4.EntityFramework.Entities;
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
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace Serede.Identidade.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClaimController : ControllerBase
{

    private readonly ApiScopeRepository _apiScopeRepository;
    private readonly ILogger<ClaimController> _logger;
    private readonly IClientStore _clients;
    private readonly IResourceStore _resources;
    private readonly ApplicationDbContext _applicationDbContext;
    public ClaimController(ApiScopeRepository apiScopeRepository, ILogger<ClaimController> logger, IClientStore clients, IResourceStore resources, ApplicationDbContext applicationDbContext)
    {
        _logger = logger;
        _apiScopeRepository = apiScopeRepository;
        _clients = clients;
        _resources = resources;
        _applicationDbContext = applicationDbContext;
    }

    [AllowAnonymous]
    [HttpGet("StringList")]
    public async Task<ActionResult> StringList()
    {
        try
        {
            var a = await _applicationDbContext.SeredeClient.Include(x => x.MngClaims).ToListAsync();


            return Ok(new ResultViewModel((await _applicationDbContext.MngClaims.Include(x=>x.Client).ToListAsync()).Select(x=>x.Name).ToArray()));
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
    [HttpGet]
    public async Task<ActionResult> GetClaims([FromQuery] UsuarioSelector val)
    {
        try
        {
            val.Busca = val.Busca == null ? "" : val.Busca;
            return Ok(new ResultViewModel(await _applicationDbContext.MngClaims.Where(x=>x.Name.Contains(val.Busca)).ToListAsync()));
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
    public async Task<ActionResult> Get(int id)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ResultViewModel(id, ModelState));

            var a = await _applicationDbContext.MngClaims.FirstOrDefaultAsync(x => x.Id == id);          
            return Ok(new ResultViewModel(a));
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
    public async Task<ActionResult> Post(MngClaim model)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ResultViewModel(model, ModelState));

            var claim = new MngClaim();

            if (model.Id != 0)
            {
                claim = await _applicationDbContext.MngClaims.FirstOrDefaultAsync(x => x.Id == model.Id);
            }            

            ResultViewModel resultModel;

            claim.Name = model.Name.Trim();
            claim.Type = model.Type.Trim();
            claim.Value = model.Value.Trim();
            claim.DefaultValue = model.DefaultValue.Trim();
            claim.ClientId = model.ClientId;
            claim.Enabled = model.Enabled;

            if (model.Id == 0)
            {
                await _applicationDbContext.MngClaims.AddAsync(claim);
            }

            var result = await _applicationDbContext.SaveChangesAsync();

            return Ok(new ResultViewModel(claim));

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