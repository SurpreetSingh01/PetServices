using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace PetServices.Services
{
    public class EmailService
    {
        private readonly string _smtpServer = "smtp.gmail.com"; // Change to your SMTP server
        private readonly int _smtpPort = 587; // Use 465 for SSL or 587 for TLS
        private readonly string _smtpUsername = "your-email@gmail.com"; // Your email
        private readonly string _smtpPassword = "your-email-password"; // Your email password or app-specific password

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_smtpUsername),
                Subject = subject,
                Body = message,
                IsBodyHtml = false
            };
            mailMessage.To.Add(email);

            var smtpClient = new SmtpClient(_smtpServer)
            {
                Port = _smtpPort,
                Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
                EnableSsl = true // Ensure SSL is enabled for a secure connection
            };

            try
            {
                await smtpClient.SendMailAsync(mailMessage);
            }
            catch (SmtpException ex)
            {
                throw new Exception($"Email failed to send. Error: {ex.Message}");
            }
        }
    }
}
