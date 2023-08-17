using Identidade.Server.Models;
using IdentityServer4.EntityFramework.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Identidade.Server.Data;

public class ApplicationDbContext : IdentityDbContext<SeredeUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {        
        modelBuilder.Entity<SeredeUser>(b =>
        {
            b.Property(x => x.FirstName)
                .HasColumnType("varchar(150)");

            b.Property(x => x.LastName)
                .HasColumnType("varchar(500)");

            b.Property(x => x.CPF)
                .HasColumnType("varchar(20)");
        });
        

        base.OnModelCreating(modelBuilder);
    }
}
//dotnet ef migrations add Identity -c ApplicationDbContext -o Migrations/Identity
//dotnet ef database update -c ApplicationDbContext

