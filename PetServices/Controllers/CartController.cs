using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using PetServices.Data;
using PetServices.Models;
using PetServices.Services;
using Stripe.Checkout;
using Stripe;

namespace PetServices.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;

        public CartController(ApplicationDbContext context, EmailService emailService, IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
        }

        // View cart
        public async Task<IActionResult> Index()
        {
            var userId = User.Identity.Name;
            var cartItems = await _context.CartItems
                .Include(c => c.Service)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            return View(cartItems);
        }

        // Add service to cart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int serviceId, int quantity = 1)
        {
            var userId = User.Identity.Name;
            if (string.IsNullOrEmpty(userId))
                return RedirectToPage("/Account/Login", new { area = "Identity" });

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.ServiceId == serviceId && c.UserId == userId);

            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
            }
            else
            {
                var service = await _context.Services.FindAsync(serviceId);
                if (service == null) return NotFound();

                cartItem = new CartItem
                {
                    ServiceId = serviceId,
                    Quantity = quantity,
                    UserId = userId
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index", "Service"); // return to services page
        }

        // Remove item from cart
        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var userId = User.Identity.Name;
            var cartItem = await _context.CartItems.FindAsync(id);
            if (cartItem != null && cartItem.UserId == userId)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Checkout page
        public async Task<IActionResult> Checkout()
        {
            var userId = User.Identity.Name;
            var cartItems = await _context.CartItems
                .Include(c => c.Service)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            return View(cartItems);
        }

        // POST: Checkout processing
        [HttpPost]
        public async Task<IActionResult> Checkout(string paymentMethod)
        {
            var userId = User.Identity.Name;
            var cartItems = await _context.CartItems
                .Include(c => c.Service)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
                return RedirectToAction(nameof(Index));

            if (paymentMethod == "Stripe")
            {
                var domain = $"{Request.Scheme}://{Request.Host}/";
                var options = new SessionCreateOptions
                {
                    SuccessUrl = domain + "Cart/Confirmation",
                    CancelUrl = domain + "Cart/Index",
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                };

                foreach (var item in cartItems)
                {
                    options.LineItems.Add(new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.Service.Price * 100), // price in cents
                            Currency = "nzd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Service.ServiceName
                            }
                        },
                        Quantity = item.Quantity
                    });
                }

                var service = new SessionService();
                var session = service.Create(options);

                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303); // Redirect to Stripe
            }

            // 🔁 Cash on Delivery
            var userEmail = userId;
            var subject = "Order Confirmation - PetServices";
            var message = "Thank you for your order! Here’s what you’ve ordered:\n\n";

            foreach (var item in cartItems)
            {
                message += $"- {item.Service.ServiceName} (x{item.Quantity})\n";
            }

            message += "\nWe’ll contact you soon to confirm your booking.";

            await _emailService.SendEmailAsync(userEmail, subject, message);

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Confirmation));
        }

        // Order success page
        public IActionResult Confirmation()
        {
            return View();
        }

        // Update cart item quantity via AJAX
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity([FromBody] CartItem updatedItem)
        {
            var userId = User.Identity.Name;
            var cartItem = await _context.CartItems.FindAsync(updatedItem.Id);

            if (cartItem == null || cartItem.UserId != userId)
                return NotFound();

            cartItem.Quantity = updatedItem.Quantity;
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
