using System.Net;
using System.Net.Mail;
using System.Text;
using GESTION_LTIPN.Models;
using Microsoft.Extensions.Options;

namespace GESTION_LTIPN.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendBookingCreatedEmailAsync(string toEmail, Booking booking, User createdByUser, Society society)
        {
            try
            {
                string subject = $"✅ Nouvelle Réservation Créée - {booking.BookingReference}";
                string body = BuildBookingCreatedEmailBody(booking, createdByUser, society);

                await SendEmailAsync(toEmail, subject, body, true);
                _logger.LogInformation($"Booking creation email sent to {toEmail} for {booking.BookingReference}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending booking creation email to {toEmail} for {booking.BookingReference}");
                // Don't throw - we don't want email failures to break booking creation
            }
        }

        public async Task SendBookingCreatedEmailAsync(List<string> toEmails, Booking booking, User createdByUser, Society society)
        {
            try
            {
                string subject = $"✅ Nouvelle Réservation Créée - {booking.BookingReference}";
                string body = BuildBookingCreatedEmailBody(booking, createdByUser, society);

                await SendEmailAsync(toEmails, subject, body, true);
                _logger.LogInformation($"Booking creation email sent to {toEmails.Count} recipients for {booking.BookingReference}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending booking creation email for {booking.BookingReference}");
                // Don't throw - we don't want email failures to break booking creation
            }
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true)
        {
            try
            {
                if (string.IsNullOrEmpty(toEmail))
                {
                    _logger.LogWarning("No email address provided");
                    return;
                }

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName);
                    message.To.Add(new MailAddress(toEmail));
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = isHtml;
                    message.Priority = MailPriority.Normal;

                    using (var smtpClient = new SmtpClient(_emailSettings.SMTPServer, _emailSettings.SMTPPort))
                    {
                        smtpClient.Credentials = new NetworkCredential(
                            _emailSettings.SMTPUsername,
                            _emailSettings.SMTPPassword
                        );
                        smtpClient.EnableSsl = _emailSettings.EnableSSL;
                        smtpClient.Timeout = 30000; // 30 seconds

                        await smtpClient.SendMailAsync(message);
                        _logger.LogInformation($"Email sent successfully to {toEmail}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email to {toEmail}");
                throw;
            }
        }

        public async Task SendEmailAsync(List<string> toEmails, string subject, string body, bool isHtml = true)
        {
            try
            {
                if (toEmails == null || !toEmails.Any())
                {
                    _logger.LogWarning("No email addresses provided");
                    return;
                }

                // Remove empty/null emails
                var validEmails = toEmails.Where(e => !string.IsNullOrEmpty(e)).ToList();

                if (!validEmails.Any())
                {
                    _logger.LogWarning("No valid email addresses provided");
                    return;
                }

                using (var message = new MailMessage())
                {
                    message.From = new MailAddress(_emailSettings.FromEmail, _emailSettings.FromName);

                    // Add all recipients
                    foreach (var email in validEmails)
                    {
                        message.To.Add(new MailAddress(email));
                    }

                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = isHtml;
                    message.Priority = MailPriority.Normal;

                    using (var smtpClient = new SmtpClient(_emailSettings.SMTPServer, _emailSettings.SMTPPort))
                    {
                        smtpClient.Credentials = new NetworkCredential(
                            _emailSettings.SMTPUsername,
                            _emailSettings.SMTPPassword
                        );
                        smtpClient.EnableSsl = _emailSettings.EnableSSL;
                        smtpClient.Timeout = 30000; // 30 seconds

                        await smtpClient.SendMailAsync(message);
                        _logger.LogInformation($"Email sent successfully to {validEmails.Count} recipients: {string.Join(", ", validEmails)}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending email to multiple recipients");
                throw;
            }
        }

        private string BuildBookingCreatedEmailBody(Booking booking, User createdByUser, Society society)
        {
            StringBuilder body = new StringBuilder();
            body.AppendLine("<!DOCTYPE html>");
            body.AppendLine("<html><head><meta charset='utf-8'></head><body>");
            body.AppendLine("<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>");

            // Header
            body.AppendLine("<div style='background-color: #0d6efd; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0;'>");
            body.AppendLine("<h2 style='margin: 0;'>✅ Nouvelle Réservation Créée</h2>");
            body.AppendLine("</div>");

            // Content
            body.AppendLine("<div style='background-color: #f8f9fa; padding: 20px; border: 1px solid #dee2e6; border-radius: 0 0 8px 8px;'>");
            body.AppendLine($"<p>Bonjour,</p>");
            body.AppendLine("<p>Une nouvelle réservation de transport a été créée dans le système.</p>");

            // Details table
            body.AppendLine("<table style='width: 100%; border-collapse: collapse; margin: 20px 0; background-color: white;'>");

            body.AppendLine("<tr style='background-color: #e9ecef;'>");
            body.AppendLine("<td style='padding: 12px; border: 1px solid #dee2e6; font-weight: bold;'>Numéro BK:</td>");
            body.AppendLine($"<td style='padding: 12px; border: 1px solid #dee2e6;'>{booking.Numero_BK}</td>");
            body.AppendLine("</tr>");

            body.AppendLine("<tr style='background-color: #e9ecef;'>");
            body.AppendLine("<td style='padding: 12px; border: 1px solid #dee2e6; font-weight: bold;'>Société:</td>");
            body.AppendLine($"<td style='padding: 12px; border: 1px solid #dee2e6;'>{society.SocietyName}</td>");
            body.AppendLine("</tr>");

            body.AppendLine("<tr>");
            body.AppendLine("<td style='padding: 12px; border: 1px solid #dee2e6; font-weight: bold;'>Type de voyage:</td>");
            body.AppendLine($"<td style='padding: 12px; border: 1px solid #dee2e6;'>{booking.TypeVoyage}</td>");
            body.AppendLine("</tr>");

            body.AppendLine("<tr style='background-color: #e9ecef;'>");
            body.AppendLine("<td style='padding: 12px; border: 1px solid #dee2e6; font-weight: bold;'>Nombre de LTC:</td>");
            body.AppendLine($"<td style='padding: 12px; border: 1px solid #dee2e6; color: #0d6efd; font-weight: bold;'>{booking.Nbr_LTC}</td>");
            body.AppendLine("</tr>");

            body.AppendLine("<tr>");
            body.AppendLine("<td style='padding: 12px; border: 1px solid #dee2e6; font-weight: bold;'>Statut:</td>");
            body.AppendLine($"<td style='padding: 12px; border: 1px solid #dee2e6;'><span style='background-color: #ffc107; color: #000; padding: 4px 8px; border-radius: 4px;'>{booking.BookingStatus}</span></td>");
            body.AppendLine("</tr>");

            body.AppendLine("<tr style='background-color: #e9ecef;'>");
            body.AppendLine("<td style='padding: 12px; border: 1px solid #dee2e6; font-weight: bold;'>Date de création:</td>");
            body.AppendLine($"<td style='padding: 12px; border: 1px solid #dee2e6;'>{booking.CreatedAt:dd/MM/yyyy HH:mm}</td>");
            body.AppendLine("</tr>");

            body.AppendLine("<tr>");
            body.AppendLine("<td style='padding: 12px; border: 1px solid #dee2e6; font-weight: bold;'>Créé par:</td>");
            body.AppendLine($"<td style='padding: 12px; border: 1px solid #dee2e6;'>{createdByUser.FullName} ({createdByUser.Username})</td>");
            body.AppendLine("</tr>");

            if (!string.IsNullOrEmpty(booking.Notes))
            {
                body.AppendLine("<tr style='background-color: #e9ecef;'>");
                body.AppendLine("<td style='padding: 12px; border: 1px solid #dee2e6; font-weight: bold;'>Notes:</td>");
                body.AppendLine($"<td style='padding: 12px; border: 1px solid #dee2e6;'>{booking.Notes}</td>");
                body.AppendLine("</tr>");
            }

            body.AppendLine("</table>");

            body.AppendLine("<div style='background-color: #fff3cd; border: 1px solid #ffecb5; border-radius: 5px; padding: 15px; margin: 20px 0;'>");
            body.AppendLine("<p style='margin: 0; color: #664d03;'><strong>📋 Prochaines étapes:</strong></p>");
            body.AppendLine("<p style='margin: 5px 0; color: #664d03;'>Cette réservation est en attente de validation. Un responsable transport devra la valider avant de pouvoir créer les voyages.</p>");
            body.AppendLine("</div>");

            body.AppendLine("<p style='margin-top: 20px;'>Cordialement,<br/>Système de Gestion LTIPN</p>");
            body.AppendLine("<p style='font-size: 12px; color: #6c757d; margin-top: 20px; border-top: 1px solid #dee2e6; padding-top: 10px;'>");
            body.AppendLine("Ceci est un email automatique, merci de ne pas y répondre directement.");
            body.AppendLine("</p>");
            body.AppendLine("</div>");
            body.AppendLine("</div>");
            body.AppendLine("</body></html>");

            return body.ToString();
        }
    }
}
