using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AutoInsuranceManagementSystem.Data;
using AutoInsuranceManagementSystem.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 1. Configure services
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

        builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
        {
            options.SignIn.RequireConfirmedAccount = false;
            options.Password.RequireDigit = true;
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
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

        var app = builder.Build();

        // Seed database with roles, admin user, and policy offerings
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
                var context = services.GetRequiredService<ApplicationDbContext>();

                await context.Database.MigrateAsync(); // Apply migrations
                await SeedData.Initialize(userManager, roleManager, context); // Seed data
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "An error occurred seeding the DB or migrating.");
            }
        }

        // 2. Configure middleware
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

// SeedData class for initializing roles, admin user, and policy offerings
public static class SeedData
{
    public static async Task Initialize(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<int>> roleManager, ApplicationDbContext context)
    {
        string[] roleNames = { "Admin", "Agent", "Customer" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<int>(roleName));
            }
        }

        // Seed Admin User
        string adminEmail = "admin@gmail.com";
        string adminPassword = "Admin1234";

        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "System Administrator",
                EmailConfirmed = true,
                Role = UserRole.ADMIN,
                Pincode = "111111",
                PhoneNumber = "7878787878",
            };

            var result = await userManager.CreateAsync(adminUser, adminPassword);
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }

        // Seed Policy Offerings
        var policyOfferings = new List<PolicyOffering>
        {
            new PolicyOffering { OfferingName = "Third-Party Bike Insurance", Description = "Covers damages caused to a third party vehicle or property by the insured bike.", CoverageAmount = 50000, CoverageType = "Third-Party", PremiumAmount = 1200, DurationInMonths = 12, IsActive = true },
            new PolicyOffering { OfferingName = "Own-Damage Bike Insurance", Description = "Provides coverage for damages to the insured bike caused by accidents, theft, or natural disasters.", CoverageAmount = 100000, CoverageType = "Own-Damage", PremiumAmount = 2500, DurationInMonths = 12, IsActive = true },
            new PolicyOffering { OfferingName = "Comprehensive Bike Insurance", Description = "Includes third-party and own-damage coverage for maximum protection.", CoverageAmount = 200000, CoverageType = "Comprehensive", PremiumAmount = 4000, DurationInMonths = 12, IsActive = true },
            new PolicyOffering { OfferingName = "Third-Party Car Insurance", Description = "Covers damages caused to a third party vehicle or property by the insured car.", CoverageAmount = 100000, CoverageType = "Third-Party", PremiumAmount = 3500, DurationInMonths = 12, IsActive = true },
            new PolicyOffering { OfferingName = "Own-Damage Car Insurance", Description = "Provides coverage for damages to the insured car due to accidents, theft, or natural calamities.", CoverageAmount = 500000, CoverageType = "Own-Damage", PremiumAmount = 7000, DurationInMonths = 12, IsActive = true },
            new PolicyOffering { OfferingName = "Comprehensive Car Insurance", Description = "Includes third-party and own-damage coverage for complete protection.", CoverageAmount = 1000000, CoverageType = "Comprehensive", PremiumAmount = 12000, DurationInMonths = 12, IsActive = true }
        };

        if (!context.PolicyOfferings.Any()) // Prevent duplicate seeding
        {
            context.PolicyOfferings.AddRange(policyOfferings);
            await context.SaveChangesAsync();
        }
    }
}