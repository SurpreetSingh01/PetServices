using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetServices.Data;
using PetServices.Models;
using System.Linq;
using System.Threading.Tasks;

namespace PetServices.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly string _userId;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
            _userId = User.Identity.Name; // Assuming username is used as userId
        }

        public async Task<IActionResult> Index()
        {
            var cartItems = await _context.CartItems
                .Include(c => c.Service)
                .Where(c => c.UserId == _userId)
                .ToListAsync();

            return View(cartItems);
        }

        public async Task<IActionResult> AddToCart(int serviceId, int quantity = 1)
        {
            var service = await _context.Services.FindAsync(serviceId);
            if (service == null)
            {
                return NotFound();
            }

            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.ServiceId == serviceId && c.UserId == _userId);

            if (cartItem != null)
            {
                cartItem.Quantity += quantity; // If the item exists, increase quantity
            }
            else
            {
                cartItem = new CartItem
                {
                    ServiceId = serviceId,
                    Quantity = quantity,
                    UserId = _userId
                };
                _context.CartItems.Add(cartItem); // If the item doesn't exist, add new
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> RemoveFromCart(int id)
        {
            var cartItem = await _context.CartItems.FindAsync(id);
            if (cartItem != null && cartItem.UserId == _userId)
            {
                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Checkout()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(string paymentMethod)
        {
            // Add your checkout logic here, like creating an order, handling payments, etc.
            // After successful payment, send an email confirmation

            var cartItems = await _context.CartItems
                .Include(c => c.Service)
                .Where(c => c.UserId == _userId)
                .ToListAsync();

            // Send email (implement email service here)
            // Clear cart after checkout
            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Confirmation));
        }

        public IActionResult Confirmation()
        {
            return View();
        }
    }
}
