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
                string subject = $"✅ Nouvelle Réservation Créée - {booking.Numero_BK}";
                string body = BuildBookingCreatedEmailBody(booking, createdByUser, society);

                await SendEmailAsync(toEmail, subject, body, true);
                _logger.LogInformation($"Booking creation email sent to {toEmail} for {booking.Numero_BK}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending booking creation email to {toEmail} for {booking.Numero_BK}");
                // Don't throw - we don't want email failures to break booking creation
            }
        }

        public async Task SendBookingCreatedEmailAsync(List<string> toEmails, Booking booking, User createdByUser, Society society)
        {
            try
            {
                string subject = $"✅ Nouvelle Réservation Créée - {booking.Numero_BK}";
                string body = BuildBookingCreatedEmailBody(booking, createdByUser, society);

                await SendEmailAsync(toEmails, subject, body, true);
                _logger.LogInformation($"Booking creation email sent to {toEmails.Count} recipients for {booking.Numero_BK}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending booking creation email for {booking.Numero_BK}");
                // Don't throw - we don't want email failures to break booking creation
            }
        }

        public async Task SendAccountCreatedEmailAsync(string toEmail, User user)
        {
            try
            {
                string subject = "🎉 Votre compte a été créé - Système de Gestion LTIPN";
                string body = BuildAccountCreatedEmailBody(user);

                await SendEmailAsync(toEmail, subject, body, true);
                _logger.LogInformation($"Account creation email sent to {toEmail} for user {user.Username}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending account creation email to {toEmail} for user {user.Username}");
                // Don't throw - we don't want email failures to break account creation
            }
        }

        public async Task SendBookingValidatedEmailAsync(string toEmail, Booking booking, User validatedByUser, Society society)
        {
            try
            {
                string subject = $"✅ Réservation Validée - {booking.Numero_BK}";
                string body = BuildBookingValidatedEmailBody(booking, validatedByUser, society);

                await SendEmailAsync(toEmail, subject, body, true);
                _logger.LogInformation($"Booking validation email sent to {toEmail} for {booking.Numero_BK}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error sending booking validation email to {toEmail} for {booking.Numero_BK}");
                // Don't throw - we don't want email failures to break booking validation
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

        private string BuildAccountCreatedEmailBody(User user)
        {
            StringBuilder body = new StringBuilder();
            body.AppendLine("<!DOCTYPE html>");
            body.AppendLine("<html><head><meta charset='utf-8'></head><body>");
            body.AppendLine("<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>");

            // Header
            body.AppendLine("<div style='background-color: #198754; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0;'>");
            body.AppendLine("<h2 style='margin: 0;'>🎉 Bienvenue au Système de Gestion LTIPN</h2>");
            body.AppendLine("</div>");

            // Content
            body.AppendLine("<div style='background-color: #f8f9fa; padding: 20px; border: 1px solid #dee2e6; border-radius: 0 0 8px 8px;'>");
            body.AppendLine($"<p>Bonjour <strong>{user.FullName}</strong>,</p>");
            body.AppendLine("<p>Votre compte a été créé avec succès dans le système de gestion LTIPN.</p>");

            // Credentials box
            body.AppendLine("<div style='background-color: #d1ecf1; border: 1px solid #bee5eb; border-radius: 5px; padding: 20px; margin: 20px 0;'>");
            body.AppendLine("<h3 style='margin-top: 0; color: #0c5460;'>📋 Vos informations de connexion</h3>");

            body.AppendLine("<table style='width: 100%; border-collapse: collapse; background-color: white; border-radius: 5px;'>");

            body.AppendLine("<tr>");
            body.AppendLine("<td style='padding: 12px; border: 1px solid #bee5eb; font-weight: bold; background-color: #e9ecef;'>Lien de connexion:</td>");
            body.AppendLine("<td style='padding: 12px; border: 1px solid #bee5eb;'><a href='http://10.77.105.112:2052' style='color: #0d6efd; text-decoration: none;'>http://10.77.105.112:2052</a></td>");
            body.AppendLine("</tr>");

            body.AppendLine("<tr>");
            body.AppendLine("<td style='padding: 12px; border: 1px solid #bee5eb; font-weight: bold; background-color: #e9ecef;'>Nom d'utilisateur:</td>");
            body.AppendLine($"<td style='padding: 12px; border: 1px solid #bee5eb;'><strong style='color: #0d6efd;'>{user.Username}</strong></td>");
            body.AppendLine("</tr>");

            body.AppendLine("<tr>");
            body.AppendLine("<td style='padding: 12px; border: 1px solid #bee5eb; font-weight: bold; background-color: #e9ecef;'>Mot de passe:</td>");
            body.AppendLine($"<td style='padding: 12px; border: 1px solid #bee5eb;'><strong style='color: #dc3545;'>{user.Password}</strong></td>");
            body.AppendLine("</tr>");

            body.AppendLine("<tr>");
            body.AppendLine("<td style='padding: 12px; border: 1px solid #bee5eb; font-weight: bold; background-color: #e9ecef;'>Rôle:</td>");
            body.AppendLine($"<td style='padding: 12px; border: 1px solid #bee5eb;'>{GetRoleName(user.Role)}</td>");
            body.AppendLine("</tr>");

            body.AppendLine("</table>");
            body.AppendLine("</div>");

            // Security warning
            body.AppendLine("<div style='background-color: #fff3cd; border: 1px solid #ffecb5; border-radius: 5px; padding: 15px; margin: 20px 0;'>");
            body.AppendLine("<p style='margin: 0; color: #664d03;'><strong>🔒 Note de sécurité:</strong></p>");
            body.AppendLine("<p style='margin: 5px 0; color: #664d03;'>Il est recommandé de changer votre mot de passe lors de votre première connexion.</p>");
            body.AppendLine("</div>");

            // Instructions
            body.AppendLine("<div style='background-color: white; border: 1px solid #dee2e6; border-radius: 5px; padding: 15px; margin: 20px 0;'>");
            body.AppendLine("<h3 style='margin-top: 0; color: #212529;'>📝 Étapes pour vous connecter:</h3>");
            body.AppendLine("<ol style='color: #495057; line-height: 1.8;'>");
            body.AppendLine("<li>Cliquez sur le lien de connexion ci-dessus</li>");
            body.AppendLine("<li>Entrez votre nom d'utilisateur et mot de passe</li>");
            body.AppendLine("<li>Accédez au système de gestion</li>");
            body.AppendLine("</ol>");
            body.AppendLine("</div>");

            body.AppendLine("<p style='margin-top: 20px;'>Si vous avez des questions ou besoin d'aide, n'hésitez pas à contacter l'administrateur système.</p>");
            body.AppendLine("<p style='margin-top: 20px;'>Cordialement,<br/>Système de Gestion LTIPN</p>");

            body.AppendLine("<p style='font-size: 12px; color: #6c757d; margin-top: 20px; border-top: 1px solid #dee2e6; padding-top: 10px;'>");
            body.AppendLine("Ceci est un email automatique, merci de ne pas y répondre directement.");
            body.AppendLine("</p>");
            body.AppendLine("</div>");
            body.AppendLine("</div>");
            body.AppendLine("</body></html>");

            return body.ToString();
        }

        private string BuildBookingValidatedEmailBody(Booking booking, User validatedByUser, Society society)
        {
            StringBuilder body = new StringBuilder();
            body.AppendLine("<!DOCTYPE html>");
            body.AppendLine("<html><head><meta charset='utf-8'></head><body>");
            body.AppendLine("<div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px;'>");

            // Header
            body.AppendLine("<div style='background-color: #198754; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0;'>");
            body.AppendLine("<h2 style='margin: 0;'>✅ Votre Réservation a été Validée</h2>");
            body.AppendLine("</div>");

            // Content
            body.AppendLine("<div style='background-color: #f8f9fa; padding: 20px; border: 1px solid #dee2e6; border-radius: 0 0 8px 8px;'>");
            body.AppendLine($"<p>Bonjour,</p>");
            body.AppendLine("<p>Nous vous informons que votre réservation de transport a été validée avec succès.</p>");

            // Details table
            body.AppendLine("<table style='width: 100%; border-collapse: collapse; margin: 20px 0; background-color: white;'>");

            body.AppendLine("<tr style='background-color: #e9ecef;'>");
            body.AppendLine("<td style='padding: 12px; border: 1px solid #dee2e6; font-weight: bold;'>Numéro BK:</td>");
            body.AppendLine($"<td style='padding: 12px; border: 1px solid #dee2e6;'><strong>{booking.Numero_BK}</strong></td>");
            body.AppendLine("</tr>");

            body.AppendLine("<tr>");
            body.AppendLine("<td style='padding: 12px; border: 1px solid #dee2e6; font-weight: bold;'>Référence:</td>");
            body.AppendLine($"<td style='padding: 12px; border: 1px solid #dee2e6;'><strong>{booking.BookingReference}</strong></td>");
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
            body.AppendLine($"<td style='padding: 12px; border: 1px solid #dee2e6; color: #198754; font-weight: bold;'>{booking.Nbr_LTC}</td>");
            body.AppendLine("</tr>");

            body.AppendLine("<tr>");
            body.AppendLine("<td style='padding: 12px; border: 1px solid #dee2e6; font-weight: bold;'>Statut:</td>");
            body.AppendLine($"<td style='padding: 12px; border: 1px solid #dee2e6;'><span style='background-color: #198754; color: white; padding: 4px 8px; border-radius: 4px;'>Validée</span></td>");
            body.AppendLine("</tr>");

            body.AppendLine("<tr style='background-color: #e9ecef;'>");
            body.AppendLine("<td style='padding: 12px; border: 1px solid #dee2e6; font-weight: bold;'>Date de validation:</td>");
            body.AppendLine($"<td style='padding: 12px; border: 1px solid #dee2e6;'>{booking.ValidatedAt:dd/MM/yyyy HH:mm}</td>");
            body.AppendLine("</tr>");

            body.AppendLine("<tr>");
            body.AppendLine("<td style='padding: 12px; border: 1px solid #dee2e6; font-weight: bold;'>Validée par:</td>");
            body.AppendLine($"<td style='padding: 12px; border: 1px solid #dee2e6;'>{validatedByUser.FullName} ({validatedByUser.Username})</td>");
            body.AppendLine("</tr>");

            if (!string.IsNullOrEmpty(booking.Notes))
            {
                body.AppendLine("<tr style='background-color: #e9ecef;'>");
                body.AppendLine("<td style='padding: 12px; border: 1px solid #dee2e6; font-weight: bold;'>Notes:</td>");
                body.AppendLine($"<td style='padding: 12px; border: 1px solid #dee2e6;'>{booking.Notes}</td>");
                body.AppendLine("</tr>");
            }

            body.AppendLine("</table>");

            body.AppendLine("<div style='background-color: #d1ecf1; border: 1px solid #bee5eb; border-radius: 5px; padding: 15px; margin: 20px 0;'>");
            body.AppendLine("<p style='margin: 0; color: #0c5460;'><strong>📋 Prochaines étapes:</strong></p>");
            body.AppendLine("<p style='margin: 5px 0; color: #0c5460;'>Les voyages seront maintenant planifiés et assignés par le responsable transport. Vous serez informé des détails de transport une fois les voyages créés.</p>");
            body.AppendLine("</div>");

            body.AppendLine("<p style='margin-top: 20px;'>Merci d'utiliser notre système de gestion.</p>");
            body.AppendLine("<p style='margin-top: 10px;'>Cordialement,<br/>Système de Gestion LTIPN</p>");

            body.AppendLine("<p style='font-size: 12px; color: #6c757d; margin-top: 20px; border-top: 1px solid #dee2e6; padding-top: 10px;'>");
            body.AppendLine("Ceci est un email automatique, merci de ne pas y répondre directement.");
            body.AppendLine("</p>");
            body.AppendLine("</div>");
            body.AppendLine("</div>");
            body.AppendLine("</body></html>");

            return body.ToString();
        }

        private string GetRoleName(string role)
        {
            return role switch
            {
                "Admin" => "Administrateur",
                "Booking_Agent" => "Agent de Réservation",
                "Trans_Respo" => "Responsable Transport",
                _ => role
            };
        }
    }
}
