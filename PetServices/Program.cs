using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PetServices.Data;
using PetServices.Services;
using Rotativa.AspNetCore;
using Stripe;

var builder = WebApplication.CreateBuilder(args);
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddTransient<EmailService>();

// Add DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

// Custom services
builder.Services.AddScoped<CartService>();

var app = builder.Build();

// Middleware
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

// Routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// ? Rotativa setup for generating PDFs
RotativaConfiguration.Setup(app.Environment.WebRootPath, "Rotativa");

// ? Seed admin
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var configuration = services.GetRequiredService<IConfiguration>();
    await DataSeeder.SeedAdminAsync(services, configuration);
}

app.Run();
