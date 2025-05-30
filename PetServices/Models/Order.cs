using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PetServices.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        public string UserId { get; set; }

        [Required]
        public DateTime OrderDate { get; set; }

        [Required]
        public decimal TotalAmount { get; set; }

        [Required]
        public string PaymentStatus { get; set; }  // e.g., "Pending", "Paid"

        public string PaymentMethod { get; set; }  // e.g., "COD", "Stripe"

        public virtual List<OrderItem> OrderItems { get; set; }
    }
}
