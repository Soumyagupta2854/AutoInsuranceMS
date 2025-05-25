using AutoInsuranceManagementSystem.Data;
using AutoInsuranceManagementSystem.Models;
using AutoInsuranceManagementSystem.ViewModels; // Assuming you might create dashboard-specific ViewModels later
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace AutoInsuranceManagementSystem.Controllers
{
    [Authorize] // All dashboard actions require login
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Dashboard (Redirects to role-specific dashboard)
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Challenge(); // Should not happen if [Authorize] is on controller
            }

            if (await _userManager.IsInRoleAsync(user, "Admin"))
            {
                return RedirectToAction(nameof(AdminDashboard));
            }
            else if (await _userManager.IsInRoleAsync(user, "Agent"))
            {
                return RedirectToAction(nameof(AgentDashboard));
            }
            else if (await _userManager.IsInRoleAsync(user, "Customer"))
            {
                return RedirectToAction(nameof(CustomerDashboard));
            }

            // Fallback or error if role is not recognized
            TempData["ErrorMessage"] = "Could not determine your user role for the dashboard.";
            return RedirectToAction("Index", "Home");
        }


        // GET: Dashboard/CustomerDashboard
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CustomerDashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Fetch data relevant to the customer
            var policies = await _context.Policies
                .Where(p => p.CustomerId == user.Id)
                .OrderByDescending(p => p.StartDate)
                .Take(3) // Show recent 3 policies
                .ToListAsync();

            var claims = await _context.Claims
                .Where(c => c.Policy.CustomerId == user.Id)
                .OrderByDescending(c => c.DateSubmitted)
                .Take(3) // Show recent 3 claims
                .Include(c => c.Policy)
                .ToListAsync();

            var payments = await _context.Payments
                .Where(p => p.Policy.CustomerId == user.Id)
                .OrderByDescending(p => p.PaymentDate)
                .Take(3)
                .Include(p => p.Policy)
                .ToListAsync();

            // You would create a ViewModel to pass this data to the view
            // For simplicity, using ViewBag for now, but ViewModel is better practice
            ViewBag.RecentPolicies = policies;
            ViewBag.RecentClaims = claims;
            ViewBag.RecentPayments = payments;
            ViewBag.CustomerName = user.FullName ?? user.UserName;

            return View();
        }

        // GET: Dashboard/AgentDashboard
        [Authorize(Roles = "Agent")]
        public async Task<IActionResult> AgentDashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Fetch data relevant to the agent
            var assignedTickets = await _context.SupportTickets
                .Where(t => t.AssignedAgentId == user.Id && (t.TicketStatus == TicketStatus.OPEN || t.TicketStatus == TicketStatus.IN_PROGRESS))
                .Include(t => t.User)
                .OrderBy(t => t.Priority)
                .ThenBy(t => t.CreatedDate)
                .Take(5)
                .ToListAsync();

            var recentClaimsAssigned = await _context.Claims
                .Where(c => c.AdjusterId == user.Id && (c.ClaimStatus == ClaimStatus.OPEN || c.ClaimStatus == ClaimStatus.UNDER_REVIEW))
                .Include(c => c.Policy)
                .OrderByDescending(c => c.DateSubmitted)
                .Take(5)
                .ToListAsync();

            ViewBag.AssignedTickets = assignedTickets;
            ViewBag.RecentClaimsAssigned = recentClaimsAssigned;
            ViewBag.AgentName = user.FullName ?? user.UserName;

            return View(); // Create AgentDashboard.cshtml
        }

        // GET: Dashboard/AdminDashboard
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Fetch data relevant to the admin
            ViewBag.TotalUsers = await _userManager.Users.CountAsync();
            ViewBag.TotalPolicies = await _context.Policies.CountAsync();
            ViewBag.TotalClaims = await _context.Claims.CountAsync();
            ViewBag.OpenTickets = await _context.SupportTickets.CountAsync(t => t.TicketStatus == TicketStatus.OPEN || t.TicketStatus == TicketStatus.IN_PROGRESS);

            ViewBag.AdminName = user.FullName ?? user.UserName;

            return View(); // Create AdminDashboard.cshtml
        }
    }
}