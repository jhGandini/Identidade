using Microsoft.AspNetCore.Identity;

namespace Identidade.Server.Models;

public class SeredeUser : IdentityUser
{
    public virtual string FirstName { get; set; }
    public virtual string LastName { get; set; }
    public virtual string CPF { get; set; }
    public virtual bool Active { get; set; }
    public virtual bool Blocked { get; set; }
    public virtual bool ServiceUser { get; set; }
    public virtual bool PasswordExpired { get; set; }
    public virtual DateTime PassExpirationDate { get; set; }



    public bool IsExpired()
    {
        if (PasswordExpired) return true;
        if (PassExpirationDate < DateTime.Now) return true;
        return false;
    }



}
