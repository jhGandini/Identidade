namespace Identidade.Server.Settings;
public class CorsSettings
{
    public string[] Origins { get; set; }
    public string[] Methods { get; set; }
    public string[] Headers { get; set; }
    public string[] ExposedHeaders { get; set; }

}

