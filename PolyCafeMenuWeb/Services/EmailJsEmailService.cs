using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using PolyCafeMenuWeb.Configuration;

namespace PolyCafeMenuWeb.Services
{
    public class EmailJsEmailService : IEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly EmailJsSettings _settings;

        public EmailJsEmailService(HttpClient httpClient, IOptions<EmailJsSettings> options)
        {
            _httpClient = httpClient;
            _settings = options.Value;
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string employeeName, string username, string newPassword)
        {
            ValidateSettings();

            var request = new EmailJsSendRequest
            {
                service_id = _settings.ServiceId,
                template_id = _settings.TemplateId,
                user_id = _settings.PublicKey,
                accessToken = string.IsNullOrWhiteSpace(_settings.PrivateKey) ? null : _settings.PrivateKey,
                template_params = new Dictionary<string, string>
                {
                    ["to_email"] = toEmail,
                    ["employee_name"] = employeeName,
                    ["username"] = username,
                    ["new_password"] = newPassword,
                    ["login_email"] = toEmail
                }
            };

            using var response = await _httpClient.PostAsJsonAsync("/api/v1.0/email/send", request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"EmailJS send failed: {(int)response.StatusCode} {error}");
            }
        }

        private void ValidateSettings()
        {
            if (string.IsNullOrWhiteSpace(_settings.ServiceId) ||
                string.IsNullOrWhiteSpace(_settings.TemplateId) ||
                string.IsNullOrWhiteSpace(_settings.PublicKey))
            {
                throw new InvalidOperationException("EmailJS settings are not configured. Please update the EmailJS section in appsettings.json.");
            }
        }

        private sealed class EmailJsSendRequest
        {
            public string service_id { get; set; } = string.Empty;
            public string template_id { get; set; } = string.Empty;
            public string user_id { get; set; } = string.Empty;
            public string? accessToken { get; set; }
            public Dictionary<string, string> template_params { get; set; } = new();
        }
    }
}
