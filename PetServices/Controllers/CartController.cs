using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetServices.Data;
using PetServices.Models;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PetServices.Services; // Make sure you add this to use EmailService

namespace PetServices.Controllers
{
    [Authorize] // Ensure only logged-in users can access the cart
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        // Inject EmailService
        public CartController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.Identity.Name;
            var cartItems = await _context.CartItems
                .Include(c => c.Service)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            return View(cartItems);
        }

        public async Task<IActionResult> AddToCart(int serviceId, int quantity = 1)
        {
            var userId = User.Identity.Name;
            var service = await _context.Services.FindAsync(serviceId);
            if (service == null)
            {
                return NotFound();
            }

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.ServiceId == serviceId && c.UserId == userId);

            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
            }
            else
            {
                cartItem = new CartItem
                {
                    ServiceId = serviceId,
                    Quantity = quantity,
                    UserId = userId
                };
                _context.CartItems.Add(cartItem);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

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

        public IActionResult Checkout()
        {
            return View(); // Display checkout form (payment method selection, etc.)
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(string paymentMethod)
        {
            var userId = User.Identity.Name;
            var cartItems = await _context.CartItems
                .Include(c => c.Service)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            // Handle the order logic here (save to Order table, process payment, etc.)
            // TODO: Add order saving logic

            // Send email confirmation to the user
            var userEmail = User.Identity.Name; // Assuming the user email is their username (could be different)
            var subject = "Order Confirmation";
            var message = "Thank you for your order. Your order details are as follows:\n";
            foreach (var item in cartItems)
            {
                message += $"{item.Service.ServiceName} - Quantity: {item.Quantity}\n";
            }
            await _emailService.SendEmailAsync(userEmail, subject, message);

            // Clear the cart after checkout
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Confirmation)); // Redirect to order confirmation page
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity([FromBody] CartItem updatedItem)
        {
            var userId = User.Identity.Name;
            var cartItem = await _context.CartItems.FindAsync(updatedItem.Id);

            if (cartItem == null || cartItem.UserId != userId)
            {
                return NotFound();
            }

            cartItem.Quantity = updatedItem.Quantity;
            await _context.SaveChangesAsync();

            return Ok();
        }

        public IActionResult Confirmation()
        {
            return View(); // Display confirmation message (could be an order number, etc.)
        }
    }
}
