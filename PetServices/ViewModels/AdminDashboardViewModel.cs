using PetServices.Models;
using System.Collections.Generic;

namespace PetServices.ViewModels
{
    public class AdminDashboardViewModel
    {
        public List<Order> Orders { get; set; } = new List<Order>();
        public int TotalOrders { get; set; }
        public int TotalUsers { get; set; }
        public decimal TotalSales { get; set; }
        public int CompletedOrders { get; set; }
        public Dictionary<string, int> OrdersByStatus { get; set; } = new();
    }
}
