using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetServices.Data;
using PetServices.Models;
using PetServices.ViewModels;

namespace PetServices.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string statusFilter, string sortOrder)
        {
            var ordersQuery = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Service)
                .AsQueryable();

            if (!string.IsNullOrEmpty(statusFilter))
            {
                ordersQuery = ordersQuery.Where(o => o.PaymentStatus == statusFilter);
            }

            ordersQuery = sortOrder switch
            {
                "date_desc" => ordersQuery.OrderByDescending(o => o.OrderDate),
                "amount_asc" => ordersQuery.OrderBy(o => o.TotalAmount),
                "amount_desc" => ordersQuery.OrderByDescending(o => o.TotalAmount),
                _ => ordersQuery.OrderBy(o => o.OrderDate),
            };

            var orders = await ordersQuery.ToListAsync();
            var totalSales = orders.Sum(o => o.TotalAmount);
            var totalUsers = await _context.Users.CountAsync();
            var completedOrders = orders.Count(o => o.PaymentStatus == "Completed");

            var ordersByStatus = orders
                .GroupBy(o => o.PaymentStatus)
                .ToDictionary(g => g.Key, g => g.Count());

            var viewModel = new AdminDashboardViewModel
            {
                Orders = orders,
                TotalOrders = orders.Count,
                TotalSales = totalSales,
                TotalUsers = totalUsers,
                CompletedOrders = completedOrders,
                OrdersByStatus = ordersByStatus
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int orderId, string newStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            order.PaymentStatus = newStatus;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}
