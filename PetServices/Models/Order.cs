using System.ComponentModel.DataAnnotations;

namespace PetServices.Models
{
    public class Order
    {
        [Key]
        public int OrderId { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string UserEmail { get; set; }  // ✅ Add this property

        [Required]
        public DateTime OrderDate { get; set; }

        [Required]
        public decimal TotalAmount { get; set; }

        [Required]
        public string PaymentStatus { get; set; }

        public string PaymentMethod { get; set; }

        public virtual List<OrderItem> OrderItems { get; set; }
    }
}
