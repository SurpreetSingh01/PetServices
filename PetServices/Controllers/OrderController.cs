using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetServices.Data;
using Rotativa.AspNetCore;
using PetServices.Models;
using System.Security.Claims;

namespace PetServices.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ Admin only
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Service)
                .ToListAsync();

            return View(orders);
        }

        // ✅ USER: My Order History
        [Authorize] // <- Must allow signed-in users
        public async Task<IActionResult> MyOrders()
        {
            var userId = User.Identity?.Name;
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Account");

            var userOrders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Service)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(userOrders);
        }
        [Authorize]
        public async Task<IActionResult> Invoice(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Service)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null || (User.IsInRole("User") && order.UserEmail != User.Identity.Name))
                return NotFound();

            return new ViewAsPdf("Invoice", order)
            {
                FileName = $"Invoice_{id}.pdf"
            };
        }
    }
}
