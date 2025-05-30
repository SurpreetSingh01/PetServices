using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetServices.Data;
using PetServices.Models;
using PetServices.ViewModels;
using SelectPdf;

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

        // ✅ ADMIN: Order Dashboard with Filters + Sorting + Stats
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index(string statusFilter, string sortOrder)
        {
            var ordersQuery = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Service)
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrEmpty(statusFilter))
            {
                ordersQuery = ordersQuery.Where(o => o.PaymentStatus == statusFilter);
            }

            // Sort options
            ordersQuery = sortOrder switch
            {
                "date_desc" => ordersQuery.OrderByDescending(o => o.OrderDate),
                "amount_asc" => ordersQuery.OrderBy(o => o.TotalAmount),
                "amount_desc" => ordersQuery.OrderByDescending(o => o.TotalAmount),
                _ => ordersQuery.OrderBy(o => o.OrderDate)
            };

            var viewModel = new AdminDashboardViewModel
            {
                Orders = await ordersQuery.ToListAsync(),
                TotalSales = await _context.Orders.SumAsync(o => o.TotalAmount),
                TotalUsers = await _context.Users.CountAsync(),
                TotalOrders = await _context.Orders.CountAsync(),
                OrdersByStatus = await _context.Orders
                    .GroupBy(o => o.PaymentStatus)
                    .ToDictionaryAsync(g => g.Key, g => g.Count())
            };

            return View(viewModel);
        }

        // ✅ ADMIN: Change Order Status
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStatus(int orderId, string newStatus)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return NotFound();

            order.PaymentStatus = newStatus;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ✅ ADMIN: Order Details View
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Service)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound();

            return View(order); // Views/Order/Details.cshtml
        }

        // ✅ USER: View their Orders
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

        // ✅ USER: View Invoice (HTML)
        public async Task<IActionResult> Invoice(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Service)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound();

            var userId = User.Identity?.Name;
            if (User.IsInRole("User") && order.UserId != userId)
                return Forbid();

            return View(order); // Views/Order/Invoice.cshtml
        }

        // ✅ USER/ADMIN: Download Invoice (PDF)
        public async Task<IActionResult> DownloadInvoice(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Service)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
                return NotFound();

            var userId = User.Identity?.Name;
            if (User.IsInRole("User") && order.UserId != userId)
                return Forbid();

            var html = await this.RenderViewAsync("Invoice", order, true);
            HtmlToPdf converter = new HtmlToPdf();
            PdfDocument doc = converter.ConvertHtmlString(html, Request.Scheme + "://" + Request.Host);

            byte[] pdf = doc.Save();
            doc.Close();

            return File(pdf, "application/pdf", $"Invoice_{id}.pdf");
        }
    }
}
