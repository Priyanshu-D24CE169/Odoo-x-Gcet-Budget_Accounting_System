namespace ShivFurnitureERP.Options;

public class SmtpOptions
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string UserName { get; set; } = "remotepriyanshu@gmail.com";
    public string Password { get; set; } = "jppw ycyo esfj dtfj";
    public string From { get; set; } = "remotepriyanshu@gmail.com";
    public string BaseUrl { get; set; } = "https://your-site";
}
