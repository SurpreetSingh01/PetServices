using Microsoft.AspNetCore.Mvc;
using PetServices.Services;

namespace PetServices.Controllers
{
    public class HomeController : Controller
    {
        private readonly EmailService _emailService;

        public HomeController(EmailService emailService)
        {
            _emailService = emailService;
        }

        public async Task<IActionResult> SendOrderConfirmationEmail(string userEmail)
        {
            string subject = "Order Confirmation";
            string body = "Thank you for your order! Your order has been confirmed.";

            await _emailService.SendEmailAsync(userEmail, subject, body);

            return View();
        }
    }
}
