using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PetServices.Data;
using PetServices.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add DbContext with connection string
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity services
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>() // If you’re using roles like Admin
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Add Email service using SendGrid
builder.Services.AddSingleton<EmailService>();  // Register EmailService for dependency injection

// Add other services as needed, for example, Cart and Order services, etc.
// builder.Services.AddScoped<CartService>();

var app = builder.Build();

// Configure middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages(); // Needed for Identity
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await DataSeeder.SeedAdminAsync(services);
}

app.Run();
