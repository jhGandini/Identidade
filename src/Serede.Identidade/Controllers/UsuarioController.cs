using IdentityServer4.Extensions;
using IdentityServer4.Models;
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
using System.Security.Claims;
using Serede.Identidade.Data;

namespace Serede.Identidade.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsuarioController : ControllerBase
{

    private readonly SignInManager<SeredeUser> _signInManager;
    private readonly UserManager<SeredeUser> _userManager;    
    private readonly ILogger<UsuarioController> _logger;
    private readonly ApplicationDbContext _applicationDbContext;
    public UsuarioController(SignInManager<SeredeUser> signInManager, ILogger<UsuarioController> logger, UserManager<SeredeUser> userManager, ApplicationDbContext applicationDbContext)
    {
        _logger = logger;
        _signInManager = signInManager;
        _userManager = userManager;
        _applicationDbContext = applicationDbContext;
    }

    [AllowAnonymous]
    [HttpGet]
    public async Task<ActionResult> GetList([FromQuery] UsuarioSelector val)
    {
        try
        {
            var busca = val.Busca.IsNullOrEmpty() ? "" : val.Busca;


            if (!ModelState.IsValid) return BadRequest(new ResultViewModel(val, ModelState));
            return Ok(new ResultViewModel(await _signInManager.UserManager.Users.Where(x => x.UserName.Contains(busca)).ToListAsync()));
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
    [HttpGet("grants")]
    public async Task<ActionResult> GetGrants()
    {
        try
        {
            var grants = new[] { GrantType.AuthorizationCode, GrantType.ClientCredentials, GrantType.DeviceFlow, GrantType.Hybrid, GrantType.Implicit, GrantType.ResourceOwnerPassword };
            return Ok(new ResultViewModel(grants));
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
    [HttpGet("{val}")]
    public async Task<ActionResult> GetById(string val)
    {
        try
        {            
            if (!ModelState.IsValid) return BadRequest(new ResultViewModel(val, ModelState));

            var user = await _signInManager.UserManager.FindByIdAsync(val);
            var claims = await _userManager.GetClaimsAsync(user);

            return Ok(new ResultViewModel(new{user=user,claims = claims }));
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
    public async Task<ActionResult> Post(CadastroUser val)
    {
        try
        {
            var result = new IdentityResult();
            ResultViewModel resultModel;
            SeredeUser user;
            var pass = "";

            if (!ModelState.IsValid) return BadRequest(new ResultViewModel(val, ModelState));

            if (val.Id.IsNullOrEmpty())
            {
                user = await montaUserAsync(val);
                pass = GenerateRandomPassword();
                result = await _userManager.CreateAsync(user, pass);

                var defaultClaims = await _applicationDbContext.MngClaims.Where(x => x.Enabled).ToListAsync();
                var a = new List<Claim>();
                foreach (var claim in defaultClaims)
                {
                    a.Add(new Claim(claim.Name, claim.DefaultValue));
                }
                await _userManager.AddClaimsAsync(user, a);

            }
            else
            {
                user = await montaUserAsync(val);                
                result = await _userManager.UpdateAsync(user);

                var claims = await _userManager.GetClaimsAsync(user);

                //Remove o que foi retirado via manager
                var remove = claims.Where(x => !val.Claims.Any(z => z.Type.Equals(x.Type))).ToList();
                await _userManager.RemoveClaimsAsync(user, remove);

                //Adiciona as novas claims via manager
                var add = val.Claims.Where(x => !claims.Any(z => z.Type.Equals(x.Type))).ToList();
                var a = new List<Claim>();
                foreach (var claim in add)
                {
                    a.Add(new Claim(claim.Type, claim.Value));
                }
                await _userManager.AddClaimsAsync(user, a);

                //Remove o que foi alterado via manager
                var up = claims.Where(x => val.Claims.Any(z => z.Type.Equals(x.Type) && !z.Value.Equals(x.Value))).ToList();
                await _userManager.RemoveClaimsAsync(user, up);

                //Recadastra o que foi alterado via manager
                var reAdd = val.Claims.Where(x => claims.Any(z => z.Type.Equals(x.Type) && !z.Value.Equals(x.Value))).ToList();
                var b = new List<Claim>();
                foreach (var claim in reAdd)
                {
                    b.Add(new Claim(claim.Type, claim.Value));
                }
                await _userManager.AddClaimsAsync(user, b);
            }           

            if (!result.Succeeded)            
                resultModel = new ResultViewModel(new { userName = user.UserName, pass = pass }, result);
            else
                resultModel = new ResultViewModel(new { userName = user.UserName, pass = pass });

            return Ok(resultModel);
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
    [HttpPost("ResetPass")]
    public async Task<ActionResult> ResetPassPost(ResetUser val)
    {
        try
        {

            if (!ModelState.IsValid) return BadRequest(new ResultViewModel(val.UserId, ModelState));

            var user = await _userManager.FindByIdAsync(val.UserId);

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var pass = GenerateRandomPassword();
            var resultReset = await _userManager.ResetPasswordAsync(user, token, pass);           

            if(resultReset.Succeeded)
                return Ok(new ResultViewModel(pass));
            else
                return Ok(new ResultViewModel("erro"));
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

    public static string GenerateRandomPassword(PasswordOptions opts = null)
    {
        if (opts == null) opts = new PasswordOptions()
        {
            RequiredLength = 8,
            RequiredUniqueChars = 4,
            RequireDigit = true,
            RequireLowercase = true,
            RequireNonAlphanumeric = true,
            RequireUppercase = true
        };

        string[] randomChars = new[] {
        "ABCDEFGHJKLMNOPQRSTUVWXYZ",    // uppercase 
        "abcdefghijkmnopqrstuvwxyz",    // lowercase
        "0123456789",                   // digits
        "!@$?_"                        // non-alphanumeric
    };
        CryptoRandom rand = new CryptoRandom();
        List<char> chars = new List<char>();

        if (opts.RequireUppercase)
            chars.Insert(rand.Next(0, chars.Count),
                randomChars[0][rand.Next(0, randomChars[0].Length)]);

        if (opts.RequireLowercase)
            chars.Insert(rand.Next(0, chars.Count),
                randomChars[1][rand.Next(0, randomChars[1].Length)]);

        if (opts.RequireDigit)
            chars.Insert(rand.Next(0, chars.Count),
                randomChars[2][rand.Next(0, randomChars[2].Length)]);

        if (opts.RequireNonAlphanumeric)
            chars.Insert(rand.Next(0, chars.Count),
                randomChars[3][rand.Next(0, randomChars[3].Length)]);

        for (int i = chars.Count; i < opts.RequiredLength
            || chars.Distinct().Count() < opts.RequiredUniqueChars; i++)
        {
            string rcs = randomChars[rand.Next(0, randomChars.Length)];
            chars.Insert(rand.Next(0, chars.Count),
                rcs[rand.Next(0, rcs.Length)]);
        }

        return new string(chars.ToArray());
    }

    private async Task<SeredeUser> montaUserAsync(SeredeUser val)
    {
        SeredeUser user;

        if (val.Id.IsNullOrEmpty())        
            user = Activator.CreateInstance<SeredeUser>();                      
        else
            user = await _userManager.FindByIdAsync(val.Id);                            
        
        user.UserName = val.UserName;
        user.CPF = val.CPF;
        user.FirstName = val.FirstName;
        user.LastName = val.LastName;
        user.Email = val.Email;
        user.PhoneNumber = val.PhoneNumber;
        user.ServiceUser = val.ServiceUser;
        user.Blocked = val.Blocked;
        user.Active = val.Active;

        return user;
    }

}


public class ResultViewModel : ResultDomain
{
    public ResultViewModel(ModelStateDictionary data)
    {
        foreach (var erro in data)
        {
            foreach (var er in erro.Value.Errors)
            {
                AddNotification(erro.Key, er.ErrorMessage);
            }
        }
    }

    public ResultViewModel(IdentityResult data)
    {
        foreach (var erro in data.Errors)
        {
            AddNotification(erro.Code, erro.Description);
        }
    }

    public ResultViewModel(object data, ModelStateDictionary state)
    {
        Data = data;

        foreach (var erro in state)
        {
            foreach (var er in erro.Value.Errors)
            {
                AddNotification(erro.Key, er.ErrorMessage);
            }
        }
    }

    public ResultViewModel(object data, IdentityResult state)
    {
        Data = data;

        foreach (var erro in state.Errors)
        {            
            AddNotification(erro.Code, erro.Description);        
        }
    }

    public ResultViewModel() { }
    public ResultViewModel(object data) : base(data) { }
    public ResultViewModel(int count) : base(count) { }
    public ResultViewModel(object data, int count) : base(data, count) { }
}

public class ResultDomain : Result
{
    public ResultDomain(object data)
    {
        Data = data;
    }

    public ResultDomain(int count)
    {
        Count = count;
    }

    public ResultDomain(object data, int count)
    {
        Data = data;
        Count = count;
    }

    public ResultDomain() { }
}

public class UsuarioSelector : Selector
{
    public string Busca { get; set; }    
}

public abstract class Selector
{
    public int Page { get; set; } = 0;
    public int Limit { get; set; } = 0;
    public string OrderBy { get; set; } = "";
    public string OrderByOrder { get; set; } = "ASC";

    public int Skip()
    {
        return (Page - 1) * Limit;
    }
}

public class ResetUser
{
    public string UserId { get; set; }
}

public class CadastroUser : SeredeUser
{
    public IEnumerable<CadastroClaim> Claims { get; set; }
}

public class CadastroClaim
{    
    public string Type { get; set; }
    public string Value { get; set; }
}