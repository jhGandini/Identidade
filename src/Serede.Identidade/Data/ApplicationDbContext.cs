using IdentityServer4.EntityFramework.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Serede.Identidade.Models;

namespace Serede.Identidade.Data;

public class ApplicationDbContext : IdentityDbContext<SeredeUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<MngClaim> MngClaims { get; set; }
    public DbSet<SeredeClient> SeredeClient { get; set; }

    public DbSet<SeredeClientRedirectUris> SeredeClientRedirectUris { get; set; }


    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<SeredeClient>(client =>
        {
            client.ToTable("Clients");
            client.HasKey(x => x.Id);

            client.Property(x => x.ClientId).HasMaxLength(200).IsRequired();
            client.Property(x => x.ClientName).HasMaxLength(200);

            client.HasMany(x => x.MngClaims)
                .WithOne(y => y.Client)
                .HasForeignKey(x => x.ClientId);

            client.HasMany(x => x.Uris)
                .WithOne(y => y.Client)
                .HasForeignKey(x => x.ClientId);
        });

        modelBuilder.Entity<SeredeClientRedirectUris>(client =>
        {
            client.ToTable("ClientRedirectUris");            
        });


        modelBuilder.Entity<SeredeUser>(b =>
        {
            b.Property(x => x.FirstName)
                .HasColumnType("varchar(150)");

            b.Property(x => x.LastName)
                .HasColumnType("varchar(500)");

            b.Property(x => x.CPF)
                .HasColumnType("varchar(20)");
        });

        modelBuilder.Entity<MngClaim>(b =>
        {
            b.ToTable("MngClaim");
            b.HasKey("Id");

            b.Property(x => x.Name)
                .HasColumnType("varchar(255)");

            b.Property(x => x.Type)
                .HasColumnType("varchar(10)");

            b.Property(x => x.Value)
                .HasColumnType("varchar(255)");

            b.Property(x => x.DefaultValue)
                .HasColumnType("varchar(255)");

        });

        base.OnModelCreating(modelBuilder);
    }
}
//dotnet ef migrations add Identity -c ApplicationDbContext -o Migrations/Identity
//dotnet ef database update -c ApplicationDbContext

