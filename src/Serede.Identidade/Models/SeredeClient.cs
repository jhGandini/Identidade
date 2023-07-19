namespace Serede.Identidade.Models;

public class SeredeClient
{
    public int Id { get; set; }
    public bool Enabled { get; set; } = true;
    public string ClientId { get; set; }
    public string ClientName { get; set; }
    public string LogoUri { get; set; }

    public ICollection<MngClaim> MngClaims { get; set; }

    public ICollection<SeredeClientRedirectUris> Uris { get; set; }
}

