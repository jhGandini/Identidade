namespace Serede.Identidade.Models;

public class SeredeClientRedirectUris
{
    public int Id { get; set; }    
    public string RedirectUri { get; set; }
    public int ClientId { get; set; }

    public SeredeClient Client { get; set; }
}

