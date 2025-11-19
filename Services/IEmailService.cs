using GESTION_LTIPN.Models;

namespace GESTION_LTIPN.Services
{
    public interface IEmailService
    {
        Task SendBookingCreatedEmailAsync(string toEmail, Booking booking, User createdByUser, Society society);
        Task SendBookingCreatedEmailAsync(List<string> toEmails, Booking booking, User createdByUser, Society society);
        Task SendAccountCreatedEmailAsync(string toEmail, User user);
        Task SendBookingValidatedEmailAsync(string toEmail, Booking booking, User validatedByUser, Society society);
        Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
        Task SendEmailAsync(List<string> toEmails, string subject, string body, bool isHtml = true);
    }
}
