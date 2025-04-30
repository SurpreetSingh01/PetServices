using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace PetServices.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Add your DbSet<T> here
        public DbSet<PetServices.Models.Services> Services { get; set; }
        public DbSet<PetServices.Models.CartItem> CartItems { get; set; }
    }
}
