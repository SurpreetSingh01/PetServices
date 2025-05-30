using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using PetServices.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PetServices.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAdminAsync(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string adminEmail = configuration["AdminCredentials:Email"];
            string adminPassword = configuration["AdminCredentials:Password"];

            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(adminUser, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        Console.WriteLine($"Error creating admin user: {error.Description}");
                    }
                }
            }
        }

        public static async Task SeedServicesAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (!await context.Services.AnyAsync())
            {
                var services = new List<Service>
                {
                    new Service { ServiceName = "Dog Walking", Description = "Daily walk for your dog", Price = 25, DurationInMinutes = 30, ImageUrl = "/images/services/dog-walking.jpg" },
                    new Service { ServiceName = "Pet Sitting", Description = "In-home pet sitting service", Price = 50, DurationInMinutes = 120, ImageUrl = "/images/services/pet-sitting.jpg" },
                    new Service { ServiceName = "Vet Visit", Description = "Vet appointment transport", Price = 40, DurationInMinutes = 60, ImageUrl = "/images/services/vet.jpg" },
                    new Service { ServiceName = "Grooming", Description = "Basic grooming for your pet", Price = 45, DurationInMinutes = 90, ImageUrl = "/images/services/grooming.jpg" },
                    new Service { ServiceName = "Pet Training", Description = "Behavioral pet training session", Price = 60, DurationInMinutes = 60, ImageUrl = "/images/services/training.jpg" }
                };

                context.Services.AddRange(services);
                await context.SaveChangesAsync();
            }
        }
    }
}
