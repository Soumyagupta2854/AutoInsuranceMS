using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoInsuranceManagementSystem.Data;
using AutoInsuranceManagementSystem.Models;
using AutoInsuranceManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AutoInsuranceManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")] // Only Admins can access this controller
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager; // Assuming int PK for IdentityRole
        private readonly ApplicationDbContext _context;


        public AdminController(UserManager<ApplicationUser> userManager,
                               RoleManager<IdentityRole<int>> roleManager,
                               ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        // GET: Admin/UserManagement
        public async Task<IActionResult> UserManagement()
        {
            var users = await _userManager.Users.ToListAsync();
            var userViewModels = new List<UserViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userViewModels.Add(new UserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    FullName = user.FullName,
                    CustomRole = user.Role, // Display the custom enum role
                    CurrentIdentityRoleName = roles.FirstOrDefault() // Display the first ASP.NET Identity role
                });
            }
            return View(userViewModels.OrderBy(u => u.UserName).ToList());
        }

        // GET: Admin/EditUserRole/5
        public async Task<IActionResult> EditUserRole(int? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound("User not found.");

            var userRoles = await _userManager.GetRolesAsync(user);

            var viewModel = new EditUserRoleViewModel
            {
                UserId = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FullName = user.FullName,
                CurrentCustomRoleName = user.Role.ToString(), // Current custom role
                NewCustomRole = user.Role, // Pre-select current custom role in dropdown
                AvailableRoles = new SelectList(Enum.GetValues(typeof(UserRole))
                                                    .Cast<UserRole>()
                                                    .Select(r => new SelectListItem { Text = r.ToString(), Value = r.ToString() }),
                                                "Value", "Text", user.Role.ToString())
            };
            return View(viewModel);
        }

        // POST: Admin/EditUserRole/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUserRole(EditUserRoleViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Repopulate AvailableRoles if returning to view due to error
                model.AvailableRoles = new SelectList(Enum.GetValues(typeof(UserRole))
                                                          .Cast<UserRole>()
                                                          .Select(r => new SelectListItem { Text = r.ToString(), Value = r.ToString() }),
                                                      "Value", "Text", model.NewCustomRole.ToString());
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId.ToString());
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction(nameof(UserManagement));
            }

            // 1. Update the custom Role property on ApplicationUser
            user.Role = model.NewCustomRole;
            var updateCustomRoleResult = await _userManager.UpdateAsync(user);

            if (!updateCustomRoleResult.Succeeded)
            {
                foreach (var error in updateCustomRoleResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                // Repopulate AvailableRoles
                model.AvailableRoles = new SelectList(Enum.GetValues(typeof(UserRole)).Cast<UserRole>().Select(r => new SelectListItem { Text = r.ToString(), Value = r.ToString() }), "Value", "Text", model.NewCustomRole.ToString());
                return View(model);
            }


            // 2. Synchronize ASP.NET Identity Roles
            var currentIdentityRoles = await _userManager.GetRolesAsync(user);
            var newIdentityRoleName = model.NewCustomRole.ToString(); // e.g., "Admin", "Agent", "Customer"

            // Ensure the target role exists as an IdentityRole
            if (!await _roleManager.RoleExistsAsync(newIdentityRoleName))
            {
                // This should ideally be handled by seeding roles at startup
                await _roleManager.CreateAsync(new IdentityRole<int>(newIdentityRoleName));
            }

            // Remove from old Identity roles (if any)
            var removalResult = await _userManager.RemoveFromRolesAsync(user, currentIdentityRoles);
            if (!removalResult.Succeeded)
            {
                TempData["ErrorMessage"] = "Error removing user from old roles.";
                // Log errors: removalResult.Errors
                return RedirectToAction(nameof(UserManagement));
            }

            // Add to new Identity role
            var additionResult = await _userManager.AddToRoleAsync(user, newIdentityRoleName);
            if (!additionResult.Succeeded)
            {
                TempData["ErrorMessage"] = $"Error adding user to new role '{newIdentityRoleName}'.";
                // Log errors: additionResult.Errors
                // Potentially try to add back to old roles as a rollback strategy, though complex.
                return RedirectToAction(nameof(UserManagement));
            }

            TempData["SuccessMessage"] = $"User '{user.UserName}' role updated successfully to {model.NewCustomRole}.";
            return RedirectToAction(nameof(UserManagement));
        }

        // TODO: Add actions for Create User (with role assignment), Delete User, Lockout management etc.
        // For simplicity, these are omitted for now but are typical for User Management.
    }
}