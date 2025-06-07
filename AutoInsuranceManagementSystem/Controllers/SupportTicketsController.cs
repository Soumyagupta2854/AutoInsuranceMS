using System;
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
    [Authorize]
    public class SupportTicketsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public SupportTicketsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: SupportTickets/MyTickets (For Customers)
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MyTickets()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var tickets = await _context.SupportTickets
                .Include(t => t.AssignedAgent)
                .Include(t => t.RelatedPolicy)
                .Where(t => t.UserId == user.Id)
                .OrderByDescending(t => t.LastUpdatedDate)
                .ToListAsync();
            return View(tickets);
        }

        // GET: SupportTickets/Create (For Customers)
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var userPolicies = await _context.Policies
                .Where(p => p.CustomerId == user.Id && (p.PolicyStatus == PolicyStatus.ACTIVE || p.PolicyStatus == PolicyStatus.PENDING_PAYMENT))
                .Select(p => new { p.PolicyId, DisplayText = $"{p.PolicyNumber} ({p.PolicyOffering!.OfferingName})" })
                .ToListAsync();

            var viewModel = new SupportTicketCreateViewModel
            {
                AvailablePolicies = new SelectList(userPolicies, "PolicyId", "DisplayText")
            };
            return View(viewModel);
        }

        // POST: SupportTickets/Create (For Customers)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Create(SupportTicketCreateViewModel viewModel)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (ModelState.IsValid)
            {
                var ticket = new SupportTicket
                {
                    Subject = viewModel.Subject,
                    IssueDescription = viewModel.IssueDescription,
                    RelatedPolicyId = viewModel.RelatedPolicyId,
                    UserId = user.Id,
                    CreatedDate = DateTime.UtcNow,
                    LastUpdatedDate = DateTime.UtcNow,
                    TicketStatus = TicketStatus.OPEN
                };

                _context.Add(ticket);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Support ticket created successfully!";
                return RedirectToAction(nameof(MyTickets));
            }

            var userPolicies = await _context.Policies
                .Where(p => p.CustomerId == user.Id && (p.PolicyStatus == PolicyStatus.ACTIVE || p.PolicyStatus == PolicyStatus.PENDING_PAYMENT))
                .Select(p => new { p.PolicyId, DisplayText = $"{p.PolicyNumber} ({p.PolicyOffering!.OfferingName})" })
                .ToListAsync();
            viewModel.AvailablePolicies = new SelectList(userPolicies, "PolicyId", "DisplayText", viewModel.RelatedPolicyId);
            return View(viewModel);
        }

        // GET: SupportTickets/Details/{id} (For All Authorized Roles)
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.SupportTickets
                .Include(t => t.User)
                .Include(t => t.AssignedAgent)
                .Include(t => t.RelatedPolicy).ThenInclude(p => p!.PolicyOffering)
                .FirstOrDefaultAsync(m => m.TicketId == id);

            if (ticket == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (User.IsInRole("Customer") && ticket.UserId != currentUser.Id)
            {
                return Forbid();
            }
            return View(ticket);
        }

        // GET: SupportTickets/AgentQueue (For Agents and Admins)
        [Authorize(Roles = "Agent,Admin")]
        public async Task<IActionResult> AgentQueue()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            IQueryable<SupportTicket> ticketsQuery = _context.SupportTickets
                .Include(t => t.User)
                .Include(t => t.RelatedPolicy)
                .Where(t => t.TicketStatus != TicketStatus.RESOLVED && t.TicketStatus != TicketStatus.CLOSED);

            if (User.IsInRole("Agent"))
            {
                ticketsQuery = ticketsQuery.Where(t => t.AssignedAgentId == user.Id);
            }

            var tickets = await ticketsQuery
                .OrderBy(t => t.Priority)
                .ThenBy(t => t.CreatedDate)
                .ToListAsync();

            return View(tickets);
        }

        // GET: SupportTickets/Update/{id} (For Agents and Admins)
        [Authorize(Roles = "Agent,Admin")]
        public async Task<IActionResult> Update(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.SupportTickets
                .Include(t => t.User)
                .Include(t => t.RelatedPolicy).ThenInclude(p => p!.PolicyOffering)
                .FirstOrDefaultAsync(t => t.TicketId == id.Value);

            if (ticket == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (User.IsInRole("Agent") && ticket.AssignedAgentId != currentUser.Id)
            {
                TempData["ErrorMessage"] = "You can only view/update tickets that are assigned to you.";
                return RedirectToAction(nameof(AgentQueue));
            }

            var viewModel = new SupportTicketUpdateViewModel
            {
                TicketId = ticket.TicketId,
                Subject = ticket.Subject,
                IssueDescription = ticket.IssueDescription,
                SubmittedByUserName = ticket.User?.UserName ?? "N/A",
                CreatedDate = ticket.CreatedDate,
                RelatedPolicyNumber = ticket.RelatedPolicy != null ? $"{ticket.RelatedPolicy.PolicyNumber} ({ticket.RelatedPolicy.PolicyOffering?.OfferingName})" : "N/A",
                TicketStatus = ticket.TicketStatus,
                Priority = ticket.Priority,
                AssignedAgentId = ticket.AssignedAgentId,
                ResolutionNotes = ticket.ResolutionNotes
            };

            // **FIX**: Populate the list of agents for BOTH roles.
            // The view needs this list to display the name of the assigned agent, even for the agent's read-only view.
            var agents = await _userManager.GetUsersInRoleAsync("Agent");
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var agentAndAdminUsers = agents.Concat(admins).OrderBy(u => u.UserName).ToList();
            viewModel.AvailableAgents = new SelectList(agentAndAdminUsers, "Id", "UserName", ticket.AssignedAgentId);

            return View(viewModel);
        }

        // POST: SupportTickets/Update/{id} (For Agents and Admins)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Agent,Admin")]
        public async Task<IActionResult> Update(int id, SupportTicketUpdateViewModel viewModel)
        {
            if (id != viewModel.TicketId) return NotFound();

            var ticketToUpdate = await _context.SupportTickets.FindAsync(id);
            if (ticketToUpdate == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (User.IsInRole("Agent"))
            {
                if (ticketToUpdate.AssignedAgentId != currentUser.Id)
                {
                    TempData["ErrorMessage"] = "You are not authorized to update this ticket.";
                    return RedirectToAction(nameof(AgentQueue));
                }
                ModelState.Remove("Priority");
                ModelState.Remove("AssignedAgentId");
                viewModel.Priority = ticketToUpdate.Priority;
                viewModel.AssignedAgentId = ticketToUpdate.AssignedAgentId;
            }

            if (ModelState.IsValid)
            {
                ticketToUpdate.TicketStatus = viewModel.TicketStatus;
                ticketToUpdate.ResolutionNotes = viewModel.ResolutionNotes;
                ticketToUpdate.LastUpdatedDate = DateTime.UtcNow;

                if (User.IsInRole("Admin"))
                {
                    ticketToUpdate.Priority = viewModel.Priority;
                    ticketToUpdate.AssignedAgentId = viewModel.AssignedAgentId;
                }

                if (viewModel.TicketStatus == TicketStatus.RESOLVED || viewModel.TicketStatus == TicketStatus.CLOSED)
                {
                    ticketToUpdate.ResolvedDate ??= DateTime.UtcNow;
                }
                else
                {
                    ticketToUpdate.ResolvedDate = null;
                }

                try
                {
                    _context.Update(ticketToUpdate);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Ticket #{ticketToUpdate.TicketId} updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    TempData["ErrorMessage"] = "Update failed. The record was modified by another user.";
                }

                if (User.IsInRole("Admin")) return RedirectToAction(nameof(AdminIndex));
                return RedirectToAction(nameof(AgentQueue));
            }

            var ticketForViewModel = await _context.SupportTickets.AsNoTracking()
                .Include(t => t.User)
                .Include(t => t.RelatedPolicy).ThenInclude(p => p!.PolicyOffering)
                .FirstAsync(t => t.TicketId == id);

            viewModel.Subject = ticketForViewModel.Subject;
            viewModel.IssueDescription = ticketForViewModel.IssueDescription;
            viewModel.SubmittedByUserName = ticketForViewModel.User?.UserName ?? "N/A";
            viewModel.CreatedDate = ticketForViewModel.CreatedDate;
            viewModel.RelatedPolicyNumber = ticketForViewModel.RelatedPolicy != null ? $"{ticketForViewModel.RelatedPolicy.PolicyNumber} ({ticketForViewModel.RelatedPolicy.PolicyOffering?.OfferingName})" : "N/A";

            var agents = await _userManager.GetUsersInRoleAsync("Agent");
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var agentAndAdminUsers = agents.Concat(admins).OrderBy(u => u.UserName).ToList();
            viewModel.AvailableAgents = new SelectList(agentAndAdminUsers, "Id", "UserName", viewModel.AssignedAgentId);

            return View(viewModel);
        }

        // GET: SupportTickets/AdminIndex (For Admins)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminIndex()
        {
            var tickets = await _context.SupportTickets
                .Include(t => t.User)
                .Include(t => t.AssignedAgent)
                .Include(t => t.RelatedPolicy).ThenInclude(p => p!.PolicyOffering)
                .OrderByDescending(t => t.LastUpdatedDate)
                .ToListAsync();
            return View(tickets);
        }

        // GET: SupportTickets/Delete/{id} (For Admins)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var ticket = await _context.SupportTickets
                .Include(t => t.User)
                .FirstOrDefaultAsync(m => m.TicketId == id);

            if (ticket == null) return NotFound();
            return View(ticket);
        }

        // POST: SupportTickets/Delete/{id} (For Admins)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var ticket = await _context.SupportTickets.FindAsync(id);
            if (ticket != null)
            {
                _context.SupportTickets.Remove(ticket);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Support ticket deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Support ticket not found.";
            }
            return RedirectToAction(nameof(AdminIndex));
        }
    }
}
