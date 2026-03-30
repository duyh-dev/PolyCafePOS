namespace PolyCafeMenuWeb.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetEmailAsync(string toEmail, string employeeName, string username, string newPassword);
    }
}
