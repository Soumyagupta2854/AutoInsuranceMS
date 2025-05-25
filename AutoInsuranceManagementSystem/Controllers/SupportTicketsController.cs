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

        // GET: SupportTickets/MyTickets (Customer)
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

        // GET: SupportTickets/Create (Customer)
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

        // POST: SupportTickets/Create (Customer)
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
                    Priority = viewModel.Priority,
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

        // GET: SupportTickets/Details/{id} (Customer, Agent, Admin)
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

        [Authorize(Roles = "Agent,Admin")]
        public async Task<IActionResult> AgentQueue()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            IQueryable<SupportTicket> ticketsQuery = _context.SupportTickets
                .Include(t => t.User)
                .Include(t => t.AssignedAgent)
                .Include(t => t.RelatedPolicy)
                .Where(t => t.TicketStatus == TicketStatus.OPEN || t.TicketStatus == TicketStatus.IN_PROGRESS || t.TicketStatus == TicketStatus.ON_HOLD);

            if (User.IsInRole("Agent"))
            {
                ticketsQuery = ticketsQuery.Where(t => t.AssignedAgentId == null || t.AssignedAgentId == user.Id);
            }

            var tickets = await ticketsQuery.OrderBy(t => t.Priority).ThenBy(t => t.CreatedDate).ToListAsync();
            return View(tickets);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Agent")]
        public async Task<IActionResult> AssignToSelf(int id)
        {
            var ticket = await _context.SupportTickets.FindAsync(id);
            var agent = await _userManager.GetUserAsync(User);

            if (ticket == null || agent == null) return NotFound();

            if (ticket.AssignedAgentId == null && ticket.TicketStatus == TicketStatus.OPEN)
            {
                ticket.AssignedAgentId = agent.Id;
                ticket.TicketStatus = TicketStatus.IN_PROGRESS;
                ticket.LastUpdatedDate = DateTime.UtcNow;
                _context.Update(ticket);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Ticket #{ticket.TicketId} assigned to you.";
            }
            else
            {
                TempData["ErrorMessage"] = $"Ticket #{ticket.TicketId} could not be assigned (already assigned or not open).";
            }
            return RedirectToAction(nameof(AgentQueue));
        }

        [Authorize(Roles = "Agent,Admin")]
        public async Task<IActionResult> Update(int? id)
        {
            if (id == null) return NotFound();
            var ticket = await _context.SupportTickets
                .Include(t => t.User)
                .Include(t => t.RelatedPolicy).ThenInclude(p => p!.PolicyOffering)
                .FirstOrDefaultAsync(t => t.TicketId == id.Value);

            if (ticket == null) return NotFound();

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

            var agents = await _userManager.Users.Where(u => u.Role == UserRole.AGENT || u.Role == UserRole.ADMIN).OrderBy(u => u.UserName).ToListAsync();
            viewModel.AvailableAgents = new SelectList(agents, "Id", "UserName", ticket.AssignedAgentId);

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Agent,Admin")]
        public async Task<IActionResult> Update(int id, SupportTicketUpdateViewModel viewModel)
        {
            if (id != viewModel.TicketId) return NotFound();

            var ticketToUpdate = await _context.SupportTickets.Include(t => t.User).Include(t => t.RelatedPolicy).ThenInclude(p => p!.PolicyOffering).FirstOrDefaultAsync(t => t.TicketId == id);
            if (ticketToUpdate == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (User.IsInRole("Agent") && ticketToUpdate.AssignedAgentId != currentUser.Id && ticketToUpdate.AssignedAgentId != null)
            {
                TempData["ErrorMessage"] = "You can only update tickets assigned to you or unassigned tickets if you assign them.";
                var agentsForDropdown = await _userManager.Users.Where(u => u.Role == UserRole.AGENT || u.Role == UserRole.ADMIN).OrderBy(u => u.UserName).ToListAsync();
                viewModel.AvailableAgents = new SelectList(agentsForDropdown, "Id", "UserName", viewModel.AssignedAgentId);
                viewModel.Subject = ticketToUpdate.Subject;
                viewModel.IssueDescription = ticketToUpdate.IssueDescription;
                viewModel.SubmittedByUserName = ticketToUpdate.User?.UserName ?? "N/A";
                viewModel.CreatedDate = ticketToUpdate.CreatedDate;
                viewModel.RelatedPolicyNumber = ticketToUpdate.RelatedPolicy != null ? $"{ticketToUpdate.RelatedPolicy.PolicyNumber} ({ticketToUpdate.RelatedPolicy.PolicyOffering?.OfferingName})" : "N/A";
                return View(viewModel);
            }

            if (ModelState.IsValid)
            {
                ticketToUpdate.TicketStatus = viewModel.TicketStatus;
                ticketToUpdate.Priority = viewModel.Priority;
                ticketToUpdate.AssignedAgentId = viewModel.AssignedAgentId;
                ticketToUpdate.ResolutionNotes = viewModel.ResolutionNotes;
                ticketToUpdate.LastUpdatedDate = DateTime.UtcNow;

                if (viewModel.TicketStatus == TicketStatus.RESOLVED || viewModel.TicketStatus == TicketStatus.CLOSED)
                {
                    ticketToUpdate.ResolvedDate = DateTime.UtcNow;
                }
                else
                {
                    ticketToUpdate.ResolvedDate = null;
                }
                if (ticketToUpdate.AssignedAgentId == currentUser.Id && ticketToUpdate.TicketStatus == TicketStatus.OPEN && viewModel.TicketStatus != TicketStatus.OPEN)
                {
                    // If agent takes action (other than just re-assigning while keeping it open) and it was OPEN, move to IN_PROGRESS
                    if (ticketToUpdate.TicketStatus != TicketStatus.RESOLVED && ticketToUpdate.TicketStatus != TicketStatus.CLOSED)
                        ticketToUpdate.TicketStatus = TicketStatus.IN_PROGRESS;
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
            var agents = await _userManager.Users.Where(u => u.Role == UserRole.AGENT || u.Role == UserRole.ADMIN).OrderBy(u => u.UserName).ToListAsync();
            viewModel.AvailableAgents = new SelectList(agents, "Id", "UserName", viewModel.AssignedAgentId);
            viewModel.Subject = ticketToUpdate.Subject;
            viewModel.IssueDescription = ticketToUpdate.IssueDescription;
            viewModel.SubmittedByUserName = ticketToUpdate.User?.UserName ?? "N/A";
            viewModel.CreatedDate = ticketToUpdate.CreatedDate;
            viewModel.RelatedPolicyNumber = ticketToUpdate.RelatedPolicy != null ? $"{ticketToUpdate.RelatedPolicy.PolicyNumber} ({ticketToUpdate.RelatedPolicy.PolicyOffering?.OfferingName})" : "N/A";
            return View(viewModel);
        }

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