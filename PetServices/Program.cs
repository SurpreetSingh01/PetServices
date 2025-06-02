using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PetServices.Data;
using PetServices.Services;
using Stripe;
//using Rotativa.AspNetCore;
using PetServices.Repositories;

var builder = WebApplication.CreateBuilder(args);

// ✅ Configure Stripe
StripeConfiguration.ApiKey = builder.Configuration["Stripe:SecretKey"];

// ✅ Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddTransient<EmailService>();
builder.Services.AddScoped<CartService>();
// Register the generic repository
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IServiceRepository, ServiceRepository>();

// ✅ Configure DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ✅ Configure Identity with roles
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

var app = builder.Build();

// ✅ Middleware pipeline
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

// ✅ Routing setup
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// ✅ Rotativa setup for PDF generation
//RotativaConfiguration.Setup(app.Environment.WebRootPath, "Rotativa");

// ✅ Run seeders: Admin + Services
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var configuration = services.GetRequiredService<IConfiguration>();

    await DataSeeder.SeedAdminAsync(services, configuration);
    await DataSeeder.SeedServicesAsync(services);
}

app.Run();
