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
            return RedirectToAction("Index", "Service");
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(string paymentMethod)
        {
            var userId = User.Identity.Name;
            var cartItems = await _context.CartItems
                .Include(c => c.Service)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            if (!cartItems.Any())
                return RedirectToAction(nameof(Index));

            // Calculate total amount
            decimal total = cartItems.Sum(item => item.Service.Price * item.Quantity);

            // Create order
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                TotalAmount = total,
                PaymentMethod = paymentMethod,
                PaymentStatus = paymentMethod == "COD" ? "Pending" : "Processing",
                OrderItems = new List<OrderItem>()
            };

            foreach (var item in cartItems)
            {
                order.OrderItems.Add(new OrderItem
                {
                    ServiceId = item.ServiceId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Service.Price
                });
            }

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Stripe Payment Flow
            if (paymentMethod == "Stripe")
            {
                var domain = $"{Request.Scheme}://{Request.Host}/";
                var options = new SessionCreateOptions
                {
                    SuccessUrl = domain + $"Cart/PaymentSuccess?orderId={order.OrderId}",
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
                            UnitAmount = (long)(item.Service.Price * 100), // cents
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
                return new StatusCodeResult(303); // Stripe redirect
            }

            // Cash on Delivery flow
            var subject = "Order Confirmation - PetServices";
            var message = "Thank you for your order!\n\n";
            foreach (var item in cartItems)
            {
                message += $"- {item.Service.ServiceName} (x{item.Quantity})\n";
            }
            message += "\nWe’ll contact you soon to confirm your booking.";

            await _emailService.SendEmailAsync(userId, subject, message);

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Confirmation), new { orderId = order.OrderId });
        }

        // Stripe: mark order as paid
        public async Task<IActionResult> PaymentSuccess(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.PaymentStatus = "Paid";

                var cartItems = await _context.CartItems
                    .Where(c => c.UserId == User.Identity.Name)
                    .ToListAsync();

                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Confirmation), new { orderId });
        }

        // Confirmation page
        public IActionResult Confirmation(int orderId)
        {
            return View(orderId);
        }

        // AJAX: update cart quantity
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
