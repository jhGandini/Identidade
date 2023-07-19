namespace Serede.Identidade.Models;

public class MngClaim
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public string Value { get; set; }
    public string DefaultValue { get; set; }
    public bool Enabled { get; set; }
    public int? ClientId { get; set; }

    public SeredeClient Client { get; set; }

}

