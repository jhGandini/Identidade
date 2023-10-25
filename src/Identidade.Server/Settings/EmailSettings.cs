namespace Identidade.Server.Settings;

public class EmailSettings
{
    public string Remetente { get; set; }
    public string Nome { get; set; }
    public string Smtp { get; set; }
    public int Porta { get; set; }
    public string CabecarioEnvio { get; set; }

}
