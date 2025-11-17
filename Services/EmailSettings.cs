namespace GESTION_LTIPN.Services
{
    public class EmailSettings
    {
        public string SMTPServer { get; set; } = string.Empty;
        public int SMTPPort { get; set; }
        public string SMTPUsername { get; set; } = string.Empty;
        public string SMTPPassword { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromName { get; set; } = string.Empty;
        public string NotificationEmail { get; set; } = string.Empty;
        public bool EnableSSL { get; set; }
    }
}
