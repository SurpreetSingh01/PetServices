using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using PetServices.Data;
using PetServices.Models;
using PetServices.Services;
using Stripe.Checkout;
using Stripe;
using Microsoft.AspNetCore.Identity;
using System.Text;

namespace PetServices.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly UserManager<IdentityUser> _userManager;

        public CartController(ApplicationDbContext context, EmailService emailService, IConfiguration configuration, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
            _userManager = userManager;
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateCart(List<CartItem> cartItems)
        {
            var userId = User.Identity.Name;

            foreach (var item in cartItems)
            {
                var cartItem = await _context.CartItems.FindAsync(item.Id);
                if (cartItem != null && cartItem.UserId == userId)
                {
                    cartItem.Quantity = item.Quantity;
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Checkout()
        {
            var userId = User.Identity.Name;
            var cartItems = await _context.CartItems
                .Include(c => c.Service)
                .Where(c => c.UserId == userId)
                .ToListAsync();

            return View(cartItems);
        }

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

            var user = await _userManager.FindByNameAsync(userId);
            var userEmail = user?.Email ?? userId;

            decimal total = cartItems.Sum(item => item.Service.Price * item.Quantity);

            var order = new Order
            {
                UserId = userId,
                UserEmail = userEmail,
                OrderDate = DateTime.Now,
                TotalAmount = total,
                PaymentMethod = paymentMethod,
                PaymentStatus = paymentMethod == "Stripe" ? "Processing" : "Pending",
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
                            UnitAmount = (long)(item.Service.Price * 100),
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
                return new StatusCodeResult(303);
            }

            var sb = new StringBuilder();
            sb.Append(@$"
<html>
<head>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: #f2f2f2;
            color: #333;
            padding: 20px;
        }}
        .container {{
            background: #ffffff;
            padding: 30px;
            border-radius: 10px;
            max-width: 650px;
            margin: auto;
            box-shadow: 0 0 15px rgba(0,0,0,0.1);
        }}
        h2 {{
            color: #0d6efd;
            margin-bottom: 10px;
        }}
        .summary {{
            margin-top: 20px;
            font-size: 14px;
        }}
        table {{
            width: 100%;
            border-collapse: collapse;
            margin-top: 20px;
        }}
        th {{
            background-color: #0d6efd;
            color: white;
            padding: 10px;
            text-align: left;
        }}
        td {{
            padding: 10px;
            border: 1px solid #e0e0e0;
            vertical-align: middle;
        }}
        .service-img {{
            border-radius: 6px;
            width: 60px;
            height: auto;
            margin-right: 8px;
            vertical-align: middle;
        }}
        .total {{
            text-align: right;
            font-weight: bold;
            font-size: 16px;
            margin-top: 10px;
            color: #28a745;
        }}
        .footer {{
            margin-top: 30px;
            font-size: 12px;
            color: #888;
            text-align: center;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <h2>🐾 PetServices - Order Confirmation</h2>
        <p>Hi {userEmail},</p>
        <p>Thank you for your order <strong>#{order.OrderId}</strong> placed on <strong>{order.OrderDate:dd MMM yyyy}</strong>.</p>

        <h4 style='margin-top: 30px;'>🧾 Ordered Services:</h4>
        <table>
            <thead>
                <tr>
                    <th>Service</th>
                    <th>Qty</th>
                    <th>Price</th>
                    <th>Total</th>
                </tr>
            </thead>
            <tbody>");

            foreach (var item in order.OrderItems)
            {
                sb.Append(@$"
                <tr>
                    <td> {item.Service?.ServiceName}</td>
                    <td>{item.Quantity}</td>
                    <td>{item.UnitPrice:C}</td>
                    <td>{(item.Quantity * item.UnitPrice):C}</td>
                </tr>");
            }

            sb.Append(@$"
            </tbody>
        </table>

        <p class='total'>Total: {order.TotalAmount:C}</p>

        <div class='summary'>
            <p>We’ll keep you updated with further details. You can also manage your orders anytime from your account dashboard.</p>
        </div>

        <div class='footer'>
            © 2025 PetServices • <a href='mailto:support@petservices.com'>support@petservices.com</a>
        </div>
    </div>
</body>
</html>");



            await _emailService.SendEmailAsync(userEmail, "Order Confirmation - PetServices", sb.ToString());

            _context.CartItems.RemoveRange(cartItems);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Confirmation), new { orderId = order.OrderId });
        }

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

        public IActionResult Confirmation(int orderId)
        {
            return View(orderId);
        }

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
