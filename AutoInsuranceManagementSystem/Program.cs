using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AutoInsuranceManagementSystem.Data;
using AutoInsuranceManagementSystem.Models;
using Microsoft.Extensions.DependencyInjection; // Required for GetRequiredService
using Microsoft.Extensions.Logging; // Required for ILogger

// If you are using the traditional Program class structure with a namespace:
// namespace AutoInsuranceManagementSystem // Or your project's root namespace
// {
public class Program
{
    public static async Task Main(string[] args) // Main method must be static
    {
        var builder = WebApplication.CreateBuilder(args);

        // 1. Add services to the container.
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // === CORRECT AND SUFFICIENT IDENTITY CONFIGURATION ===
        builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>(options => {
            options.SignIn.RequireConfirmedAccount = false; // Set to false for easier testing, true for production
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            // options.User.RequireUniqueEmail = true; // Good practice
        })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .AddDefaultUI();

        builder.Services.AddControllersWithViews();
        builder.Services.AddRazorPages();

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Identity/Account/Login";
            options.LogoutPath = "/Identity/Account/Logout";
            options.AccessDeniedPath = "/Identity/Account/AccessDenied";
        });

        // For file uploads, you might want to configure Kestrel's MaxRequestBodySize if dealing with large files
        // builder.WebHost.ConfigureKestrel(serverOptions =>
        // {
        //     serverOptions.Limits.MaxRequestBodySize = 100 * 1024 * 1024; // e.g., 100 MB
        // });

        var app = builder.Build();

        // Seed database with roles and default admin user
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
                var context = services.GetRequiredService<ApplicationDbContext>();

                await context.Database.MigrateAsync(); // Ensure migrations are applied

                await SeedData.Initialize(userManager, roleManager); // Call your seeding method
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred seeding the DB or migrating.");
            }
        }

        // 2. Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/Home/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
        app.MapRazorPages();

        app.Run();
    }
}

// Define a static class for seeding data if you haven't already
// Ensure this class is accessible, e.g., in the same namespace or add a using directive
public static class SeedData
{
    public static async Task Initialize(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<int>> roleManager)
    {
        string[] roleNames = { "Admin", "Agent", "Customer" };
        IdentityResult roleResult;

        foreach (var roleName in roleNames)
        {
            var roleExist = await roleManager.RoleExistsAsync(roleName);
            if (!roleExist)
            {
                roleResult = await roleManager.CreateAsync(new IdentityRole<int>(roleName));
                // You might want to log if role creation fails, e.g., using ILogger
                // if (!roleResult.Succeeded) { /* Log errors */ }
            }
        }

        // Create a default Admin user (ensure this runs only once or checks for existence)
        // Consider making the admin email and password configurable or more secure for initial setup
        string adminEmail = "admin@yourapp.com"; // CHANGE THIS to your desired admin email
        string adminPassword = "AdminPassword123!"; // CHANGE THIS to a very strong password

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                EmailConfirmed = true, // Set to true to bypass email confirmation for the initial admin
                Role = UserRole.ADMIN, // Your custom enum
                                       // Set other custom properties if needed for ApplicationUser
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin"); // Add to ASP.NET Core Identity "Admin" role
            }
            // else: Log user creation errors from result.Errors
        }
    }
}
// } // End of namespace if you used one for Program class
