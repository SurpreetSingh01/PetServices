using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PetServices.Models
{
    public class CartItem
    {
        [Key]
        public int Id { get; set; }

        public int ServiceId { get; set; }

        [ForeignKey("ServiceId")]
        public Service Service { get; set; }

        public string UserId { get; set; }

        public int Quantity { get; set; }
    }
}
