# Email Notification Configuration

## Overview

The system automatically sends email notifications when new bookings are created. This document describes the email configuration and functionality.

---

## Configuration

### Email Settings (appsettings.json)

The email settings are configured in `appsettings.json`:

```json
{
  "EmailSettings": {
    "SMTPServer": "smtp.gmail.com",
    "SMTPPort": 587,
    "SMTPUsername": "a.elatmani@kingpelagique.ma",
    "SMTPPassword": "kryiodkyihwtntvn",
    "FromEmail": "a.elatmani@kingpelagique.ma",
    "FromName": "Système de Gestion LTIPN",
    "NotificationEmail": "saad.ourami@kingpelagique.ma",
    "EnableSSL": true
  }
}
```

### Configuration Parameters

| Parameter | Description |
|-----------|-------------|
| `SMTPServer` | Gmail SMTP server address |
| `SMTPPort` | SMTP port (587 for TLS) |
| `SMTPUsername` | Email account username |
| `SMTPPassword` | App-specific password for Gmail |
| `FromEmail` | Sender email address |
| `FromName` | Display name for sender |
| `NotificationEmail` | Email address to receive booking notifications (saad.ourami@kingpelagique.ma) |
| `EnableSSL` | Enable SSL/TLS encryption |

---

## Email Notifications

### When are Emails Sent?

Emails are automatically sent when:
- ✅ **New Booking Created** - A notification is sent to `saad.ourami@kingpelagique.ma` with complete booking details

### Email Content

The booking creation email includes:

#### Header Information
- Subject: `✅ Nouvelle Réservation Créée - {BookingReference}`
- Professional HTML formatted email with responsive design

#### Booking Details
- **Référence de réservation**: Unique booking reference (e.g., BK20251117001)
- **Numéro BK**: Manual booking number
- **Société**: Client company name
- **Type de voyage**: Transport type (Congolé/DRY)
- **Nombre de LTC**: Number of transport lots
- **Statut**: Booking status (Pending, Validated, etc.)
- **Date de création**: Creation date and time
- **Créé par**: Full name and username of creator
- **Notes**: Any additional notes (if provided)

#### Contact Information
- Creator's email address
- Society phone number (if available)

#### Next Steps
- Information about validation workflow
- Reminder that voyages can be created after validation

---

## Architecture

### Service Classes

#### 1. **EmailSettings.cs**
Configuration model that maps to `appsettings.json`:
```csharp
public class EmailSettings
{
    public string SMTPServer { get; set; }
    public int SMTPPort { get; set; }
    public string SMTPUsername { get; set; }
    public string SMTPPassword { get; set; }
    public string FromEmail { get; set; }
    public string FromName { get; set; }
    public string NotificationEmail { get; set; }
    public bool EnableSSL { get; set; }
}
```

#### 2. **IEmailService.cs**
Interface for email service:
```csharp
public interface IEmailService
{
    Task SendBookingCreatedEmailAsync(Booking booking, User createdByUser, Society society);
    Task SendEmailAsync(string toEmail, string subject, string body, bool isHtml = true);
}
```

#### 3. **EmailService.cs**
Implementation of email sending logic:
- Uses `System.Net.Mail.SmtpClient`
- HTML email template builder
- Error handling and logging
- Async/await pattern for non-blocking operations

### Dependency Injection

Email service is registered in `Program.cs`:
```csharp
// Configure Email Settings
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));

// Register Email Service
builder.Services.AddScoped<IEmailService, EmailService>();
```

### Controller Integration

In `BookingController.cs`, the email service is injected and called after booking creation:
```csharp
public BookingController(ApplicationDbContext context, ILogger<BookingController> logger, IEmailService emailService)
{
    _context = context;
    _logger = logger;
    _emailService = emailService;
}

// In Create POST action:
await _emailService.SendBookingCreatedEmailAsync(booking, createdByUser, society);
```

---

## Error Handling

### Resilience Features

1. **Non-Blocking**: Email failures don't prevent booking creation
2. **Logging**: All email operations are logged (success and failures)
3. **Try-Catch**: Email sending is wrapped in try-catch to handle exceptions
4. **Graceful Degradation**: User sees success message even if email fails

### Log Messages

```csharp
// Success
_logger.LogInformation("Email notification sent for booking {BookingReference}", bookingReference);

// Failure
_logger.LogError(ex, "Failed to send email notification for booking {BookingReference}", bookingReference);
```

---

## Gmail Configuration

### App-Specific Password

⚠️ **Important**: The SMTP password is an **App-Specific Password**, not the regular Gmail password.

To generate an App-Specific Password:
1. Go to Google Account settings
2. Security → 2-Step Verification
3. App passwords
4. Generate a new password for "Mail"
5. Use this password in `SMTPPassword`

### SMTP Settings for Gmail

- **Server**: smtp.gmail.com
- **Port**: 587 (TLS/STARTTLS)
- **Encryption**: TLS enabled
- **Authentication**: Required

---

## Testing

### Manual Testing

To test email functionality:
1. Create a new booking through the UI
2. Check server logs for email sending confirmation
3. Verify email received at `saad.ourami@kingpelagique.ma`

### Test Email Content

Expected email format:
- ✅ Professional header with blue background
- 📋 Complete booking information table
- 📧 Contact details section
- 📝 Next steps information
- 🎨 Responsive HTML design

---

## Troubleshooting

### Common Issues

#### 1. Email Not Sent
**Symptoms**: Booking created but no email received

**Possible Causes**:
- Invalid SMTP credentials
- Gmail blocking "less secure apps"
- Network/firewall issues
- Incorrect notification email address

**Solutions**:
- Check server logs for error messages
- Verify SMTP settings in appsettings.json
- Ensure App-Specific Password is valid
- Check spam/junk folder

#### 2. Authentication Errors
**Symptoms**: SMTP authentication failed

**Solutions**:
- Regenerate App-Specific Password
- Verify SMTPUsername matches FromEmail
- Check 2-Step Verification is enabled

#### 3. Connection Timeout
**Symptoms**: SMTP connection timeout

**Solutions**:
- Verify network connectivity
- Check firewall allows port 587
- Ensure EnableSSL is set to true

---

## Future Enhancements

Potential improvements:
- [ ] Email templates for other events (validation, voyage creation, etc.)
- [ ] Configurable email recipients per event type
- [ ] Email queue for high-volume scenarios
- [ ] Email delivery status tracking
- [ ] Support for multiple notification recipients
- [ ] Attachment support (PDF reports, etc.)
- [ ] Email templates in database for easy editing
- [ ] Multi-language email support

---

## Security Considerations

⚠️ **Security Notes**:
1. SMTP password is stored in appsettings.json
2. Consider using **User Secrets** in development
3. Use **Azure Key Vault** or environment variables in production
4. Never commit passwords to source control
5. Use App-Specific Passwords instead of account passwords
6. Enable 2-Step Verification on sender account

### Recommended: Use User Secrets

In development, use User Secrets instead of appsettings.json:

```bash
dotnet user-secrets set "EmailSettings:SMTPPassword" "your-app-password"
```

---

## Files Created/Modified

### New Files
- `Services/EmailSettings.cs` - Email configuration model
- `Services/IEmailService.cs` - Email service interface
- `Services/EmailService.cs` - Email service implementation
- `EMAIL_CONFIGURATION.md` - This documentation

### Modified Files
- `appsettings.json` - Added EmailSettings section
- `Program.cs` - Registered EmailService
- `Controllers/BookingController.cs` - Integrated email sending

---

**Last Updated**: 2025-11-17
