namespace PolyCafeMenuWeb.Configuration
{
    public class EmailJsSettings
    {
        public string BaseUrl { get; set; } = "https://api.emailjs.com";
        public string ServiceId { get; set; } = string.Empty;
        public string TemplateId { get; set; } = string.Empty;
        public string PublicKey { get; set; } = string.Empty;
        public string PrivateKey { get; set; } = string.Empty;
    }
}
