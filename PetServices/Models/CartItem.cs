namespace PetServices.Models
{
    public class CartItem
    {
        public int Id { get; set; }

        public int ServiceId { get; set; }
        public Services Service { get; set; }

        public string UserId { get; set; }
        public int Quantity { get; set; }
    }
}
