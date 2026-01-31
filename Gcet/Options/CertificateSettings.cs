namespace Gcet.Options;

public class CertificateSettings
{
    public string? Path { get; set; } = "certs/gcet.pfx";
    public string? Password { get; set; } = "Strong@123";
    public string? StoreName { get; set; } = "";
    public string? StoreLocation { get; set; } = "";
    public string? Thumbprint { get; set; } = "";
}
