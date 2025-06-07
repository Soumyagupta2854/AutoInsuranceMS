using System;
using System.Linq;
using System.Security.Claims;
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
    public class PoliciesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PoliciesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Policies (For Admin/Agent - lists *instantiated* customer policies)
        [Authorize(Roles = "Admin,Agent")]
        public async Task<IActionResult> Index()
        {
            var policies = await _context.Policies
                .Include(p => p.Customer)
                .Include(p => p.PolicyOffering)
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();
            return View(policies);
        }

        // GET: Policies/MyPolicies (For Customer)
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MyPolicies()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var customerPolicies = await _context.Policies
                .Include(p => p.PolicyOffering)
                .Where(p => p.CustomerId.ToString() == userId)
                .OrderByDescending(p => p.StartDate)
                .ToListAsync();
            return View(customerPolicies);
        }

        // GET: Policies/AvailableOfferings (For Customer to view and select an offering)
        [Authorize(Roles = "Customer,Admin,Agent")]
        public async Task<IActionResult> AvailableOfferings()
        {
            var offerings = await _context.PolicyOfferings
                                        .Where(po => po.IsActive)
                                        .OrderBy(po => po.OfferingName)
                                        .ToListAsync();
            return View(offerings);
        }

        // GET: Policies/PurchaseConfirmation/{offeringId} (Customer confirms purchase details)
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> PurchaseConfirmation(int? offeringId)
        {
            if (offeringId == null) return NotFound();

            var offering = await _context.PolicyOfferings.FindAsync(offeringId);
            if (offering == null || !offering.IsActive)
            {
                TempData["ErrorMessage"] = "Selected policy offering is not available.";
                return RedirectToAction(nameof(AvailableOfferings));
            }

            var model = new PolicyPurchaseViewModel
            {
                PolicyOfferingId = offering.PolicyOfferingId,
                OfferingName = offering.OfferingName,
                Description = offering.Description,
                CoverageAmount = offering.CoverageAmount,
                PremiumAmount = offering.PremiumAmount,
                CoverageType = offering.CoverageType,
                DurationInMonths = offering.DurationInMonths,
                StartDate = DateTime.Today,
                VehicleYear = DateTime.Now.Year // Default the year for user convenience
            };
            return View(model);
        }

        // POST: Policies/Purchase (Customer completes purchase)
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Purchase(PolicyPurchaseViewModel purchaseModel)
        {
            if (ModelState.IsValid)
            {
                var offering = await _context.PolicyOfferings.FindAsync(purchaseModel.PolicyOfferingId);
                var customer = await _userManager.GetUserAsync(User);

                if (offering == null || !offering.IsActive || customer == null)
                {
                    TempData["ErrorMessage"] = "There was an issue processing your request. Please try again.";
                    return RedirectToAction(nameof(AvailableOfferings));
                }

                if (purchaseModel.StartDate < DateTime.Today)
                {
                    ModelState.AddModelError("StartDate", "Policy start date cannot be in the past.");
                }

                if (!ModelState.IsValid)
                {
                    // Repopulate non-posted parts of the view model if validation fails
                    purchaseModel.OfferingName = offering.OfferingName;
                    purchaseModel.Description = offering.Description;
                    purchaseModel.CoverageType = offering.CoverageType;
                    purchaseModel.DurationInMonths = offering.DurationInMonths;
                    return View("PurchaseConfirmation", purchaseModel);
                }

                // **UPDATED LOGIC**: Combine structured vehicle data into a single string for storage.
                string vehicleDetailsString = $"{purchaseModel.VehicleMake} {purchaseModel.VehicleModel} ({purchaseModel.VehicleYear}), Reg No: {purchaseModel.VehicleRegistrationNumber.ToUpper()}";

                var newPolicy = new Policy
                {
                    PolicyOfferingId = offering.PolicyOfferingId,
                    CustomerId = customer.Id,
                    PolicyNumber = $"POL-{DateTime.UtcNow.Ticks.ToString().Substring(8)}",
                    VehicleDetails = vehicleDetailsString, // Use the combined string here
                    ActualCoverageAmount = offering.CoverageAmount,
                    ActualPremiumAmount = offering.PremiumAmount,
                    StartDate = purchaseModel.StartDate,
                    EndDate = purchaseModel.StartDate.AddMonths(offering.DurationInMonths),
                    PolicyStatus = PolicyStatus.PENDING_PAYMENT
                };

                _context.Policies.Add(newPolicy);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Policy '{offering.OfferingName}' selection confirmed! Please proceed to payment.";
                return RedirectToAction("InitiatePolicyPayment", "Payments", new { policyId = newPolicy.PolicyId });
            }

            // If we get here, something failed; redisplay form
            if (purchaseModel.PolicyOfferingId > 0)
            {
                var offeringForView = await _context.PolicyOfferings.FindAsync(purchaseModel.PolicyOfferingId);
                if (offeringForView != null)
                {
                    purchaseModel.OfferingName = offeringForView.OfferingName;
                    purchaseModel.Description = offeringForView.Description;
                    purchaseModel.CoverageType = offeringForView.CoverageType;
                    purchaseModel.DurationInMonths = offeringForView.DurationInMonths;
                }
            }
            return View("PurchaseConfirmation", purchaseModel);
        }


        // GET: Policies/Details/5
        [Authorize(Roles = "Admin,Agent,Customer")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var policy = await _context.Policies
                .Include(p => p.Customer)
                .Include(p => p.PolicyOffering)
                .FirstOrDefaultAsync(m => m.PolicyId == id);

            if (policy == null) return NotFound();

            if (User.IsInRole("Customer"))
            {
                var currentUserId = _userManager.GetUserId(User);
                if (policy.CustomerId.ToString() != currentUserId)
                {
                    return Forbid();
                }
            }
            return View(policy);
        }

        // Admin can still edit certain aspects of an *instantiated* policy
        // GET: Policies/EditInstantiatedPolicy/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditInstantiatedPolicy(int? id)
        {
            if (id == null) return NotFound();
            var policy = await _context.Policies.Include(p => p.Customer).Include(p => p.PolicyOffering).FirstOrDefaultAsync(p => p.PolicyId == id);
            if (policy == null) return NotFound();
            return View(policy);
        }

        // POST: Policies/EditInstantiatedPolicy/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditInstantiatedPolicy(int id, [Bind("PolicyId,PolicyNumber,PolicyOfferingId,CustomerId,VehicleDetails,ActualCoverageAmount,ActualPremiumAmount,StartDate,EndDate,PolicyStatus")] Policy policy)
        {
            if (id != policy.PolicyId) return NotFound();

            var originalPolicy = await _context.Policies.AsNoTracking().FirstOrDefaultAsync(p => p.PolicyId == id);
            if (originalPolicy == null) return NotFound();

            policy.PolicyOfferingId = originalPolicy.PolicyOfferingId;
            policy.CustomerId = originalPolicy.CustomerId;
            policy.PolicyNumber = originalPolicy.PolicyNumber;

            if (ModelState.IsValid)
            {
                if (policy.EndDate <= policy.StartDate)
                {
                    ModelState.AddModelError("EndDate", "End date must be after the start date.");
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        _context.Update(policy);
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Instantiated policy updated successfully!";
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!PolicyExists(policy.PolicyId)) return NotFound();
                        TempData["ErrorMessage"] = "Could not update policy. The record was modified by another user.";
                    }
                    return RedirectToAction(nameof(Index));
                }
            }
            policy.Customer = await _context.Users.FindAsync(policy.CustomerId);
            policy.PolicyOffering = await _context.PolicyOfferings.FindAsync(policy.PolicyOfferingId);
            return View(policy);
        }


        // Admin can delete an *instantiated* policy (e.g., created in error)
        // GET: Policies/DeleteInstantiatedPolicy/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteInstantiatedPolicy(int? id)
        {
            if (id == null) return NotFound();
            var policy = await _context.Policies.Include(p => p.Customer).Include(p => p.PolicyOffering).FirstOrDefaultAsync(m => m.PolicyId == id);
            if (policy == null) return NotFound();
            return View(policy);
        }

        // POST: Policies/DeleteInstantiatedPolicy/5
        [HttpPost, ActionName("DeleteInstantiatedPolicy")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteInstantiatedPolicyConfirmed(int id)
        {
            var policy = await _context.Policies.FindAsync(id);
            if (policy != null)
            {
                var hasClaims = await _context.Claims.AnyAsync(c => c.PolicyId == id);
                var hasPayments = await _context.Payments.AnyAsync(p => p.PolicyId == id);

                if (hasClaims || hasPayments)
                {
                    TempData["ErrorMessage"] = "Cannot delete policy: it has associated claims or payments. Consider cancelling it instead.";
                    return RedirectToAction(nameof(Index));
                }
                _context.Policies.Remove(policy);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Instantiated policy deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Policy not found.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PolicyExists(int id) => _context.Policies.Any(e => e.PolicyId == id);
    }
}
