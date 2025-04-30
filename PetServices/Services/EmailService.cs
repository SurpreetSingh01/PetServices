using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace PetServices.Services
{
    public class EmailService
    {
        private readonly string _apiKey;

        public EmailService(IConfiguration configuration)
        {
            _apiKey = configuration["SendGrid:ApiKey"]; // Get the API key from configuration
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body)
        {
            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress("no-reply@petservices.com", "Pet Services");
            var to = new EmailAddress(toEmail);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, body, body);

            var response = await client.SendEmailAsync(msg);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                // Handle failure
                throw new Exception($"Failed to send email. Status code: {response.StatusCode}");
            }
        }
    }
}
