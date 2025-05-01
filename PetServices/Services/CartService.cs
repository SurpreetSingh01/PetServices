using Microsoft.EntityFrameworkCore;
using PetServices.Data;
using PetServices.Models;

namespace PetServices.Services
{
    public class CartService
    {
        private readonly ApplicationDbContext _context;

        public CartService(ApplicationDbContext context)
        {
            _context = context;
        }

        // Add methods like AddToCart, RemoveFromCart, etc.
        public async Task AddToCartAsync(string userId, int serviceId, int quantity)
        {
            var cartItem = await _context.CartItems
                .FirstOrDefaultAsync(c => c.ServiceId == serviceId && c.UserId == userId);

            if (cartItem != null)
            {
                cartItem.Quantity += quantity;  // Increase quantity if already exists
            }
            else
            {
                cartItem = new CartItem
                {
                    UserId = userId,
                    ServiceId = serviceId,
                    Quantity = quantity
                };
                _context.CartItems.Add(cartItem);  // Add new cart item
            }

            await _context.SaveChangesAsync();
        }
    }
}
