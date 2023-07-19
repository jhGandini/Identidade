using IdentityServer4.Models;

namespace Serede.Identidade.Models;

public class ResourceCadastro : Resource
{
    public int Id { get; set; }
    public string Type { get; set; }
    public string Claims { get; set; }
}

