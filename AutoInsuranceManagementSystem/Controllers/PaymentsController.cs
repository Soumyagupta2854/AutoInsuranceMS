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
    public class PaymentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public PaymentsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Payments (Admin/Agent view all payments)
        [Authorize(Roles = "Admin,Agent")]
        public async Task<IActionResult> Index()
        {
            var payments = await _context.Payments
                .Include(p => p.Policy)
                    .ThenInclude(pol => pol!.Customer)
                .Include(p => p.Policy)
                    .ThenInclude(pol => pol!.PolicyOffering)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
            return View(payments);
        }

        // GET: Payments/MyPayments (Customer view their payments)
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MyPayments()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var payments = await _context.Payments
                .Include(p => p.Policy)
                    .ThenInclude(pol => pol!.PolicyOffering)
                .Where(p => p.Policy != null && p.Policy.CustomerId == user.Id)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync();
            return View(payments);
        }

        // GET: Payments/Details/5
        [Authorize(Roles = "Admin,Agent,Customer")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var payment = await _context.Payments
                .Include(p => p.Policy)
                    .ThenInclude(pol => pol!.Customer)
                .Include(p => p.Policy)
                    .ThenInclude(pol => pol!.PolicyOffering)
                .FirstOrDefaultAsync(m => m.PaymentId == id);

            if (payment == null) return NotFound();

            if (User.IsInRole("Customer"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null || payment.Policy?.CustomerId != user.Id)
                {
                    return Forbid();
                }
            }
            return View(payment);
        }

        // GET: Payments/InitiatePolicyPayment/{policyId}
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> InitiatePolicyPayment(int? policyId)
        {
            if (policyId == null) return NotFound();

            var policy = await _context.Policies
                .Include(p => p.PolicyOffering)
                .FirstOrDefaultAsync(p => p.PolicyId == policyId);

            if (policy == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null || policy.CustomerId != user.Id)
            {
                return Forbid("You are not authorized to make payments for this policy.");
            }

            if (policy.PolicyStatus == PolicyStatus.ACTIVE)
            {
                TempData["InfoMessage"] = $"Policy {policy.PolicyNumber} is already active.";
                return RedirectToAction("MyPolicies", "Policies");
            }
            if (policy.PolicyStatus != PolicyStatus.PENDING_PAYMENT)
            {
                TempData["ErrorMessage"] = $"Policy {policy.PolicyNumber} is not currently awaiting payment.";
                return RedirectToAction("MyPolicies", "Policies");
            }


            var viewModel = new PaymentViewModel
            {
                PolicyId = policy.PolicyId,
                PolicyNumber = policy.PolicyNumber,
                CustomerName = user.FullName ?? user.UserName,
                OfferingName = policy.PolicyOffering?.OfferingName,
                Amount = policy.ActualPremiumAmount,
                PaymentDate = DateTime.UtcNow,
                Method = PaymentMethod.SIMULATED_CARD
            };
            return View(viewModel);
        }

        // POST: Payments/ProcessPolicyPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> ProcessPolicyPayment(PaymentViewModel paymentViewModel)
        {
            var policy = await _context.Policies
                .Include(p => p.PolicyOffering)
                .FirstOrDefaultAsync(p => p.PolicyId == paymentViewModel.PolicyId);
            var user = await _userManager.GetUserAsync(User);

            if (policy == null || user == null || policy.CustomerId != user.Id)
            {
                TempData["ErrorMessage"] = "Invalid policy or user for payment.";
                return RedirectToAction("MyPolicies", "Policies");
            }
            if (policy.PolicyStatus == PolicyStatus.ACTIVE)
            {
                TempData["InfoMessage"] = $"Policy {policy.PolicyNumber} is already active.";
                return RedirectToAction("MyPolicies", "Policies");
            }
            if (policy.PolicyStatus != PolicyStatus.PENDING_PAYMENT)
            {
                TempData["ErrorMessage"] = $"Policy {policy.PolicyNumber} is not currently awaiting payment.";
                return RedirectToAction("MyPolicies", "Policies");
            }

            if (paymentViewModel.Method == PaymentMethod.SIMULATED_CARD)
            {
                if (string.IsNullOrWhiteSpace(paymentViewModel.CardNumber)) ModelState.AddModelError("CardNumber", "Card number is required.");
                if (string.IsNullOrWhiteSpace(paymentViewModel.ExpiryDate)) ModelState.AddModelError("ExpiryDate", "Expiry date is required.");
                if (string.IsNullOrWhiteSpace(paymentViewModel.CVV)) ModelState.AddModelError("CVV", "CVV is required.");
            }

            if (paymentViewModel.Amount != policy.ActualPremiumAmount)
            {
                ModelState.AddModelError("Amount", "Payment amount does not match the policy premium.");
            }


            if (ModelState.IsValid)
            {
                var payment = new Payment
                {
                    PolicyId = policy.PolicyId,
                    Amount = policy.ActualPremiumAmount,
                    PaymentDate = DateTime.UtcNow,
                    Method = paymentViewModel.Method,
                    Status = PaymentStatus.SUCCESS,
                    TransactionId = $"SIM_TRANS_{Guid.NewGuid().ToString().Substring(0, 12).ToUpper()}"
                };

                _context.Payments.Add(payment);
                policy.PolicyStatus = PolicyStatus.ACTIVE;
                _context.Update(policy);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Payment for policy {policy.PolicyNumber} was successful. Your policy is now active!";
                return RedirectToAction("MyPolicies", "Policies");
            }

            paymentViewModel.PolicyNumber = policy.PolicyNumber;
            paymentViewModel.CustomerName = user.FullName ?? user.UserName;
            paymentViewModel.OfferingName = policy.PolicyOffering?.OfferingName;
            return View("InitiatePolicyPayment", paymentViewModel);
        }

        // GET: Payments/RecordManualPayment
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RecordManualPayment(int? policyId)
        {
            var viewModel = new PaymentViewModel
            {
                Method = PaymentMethod.MANUAL_OFFLINE,
                Status = PaymentStatus.SUCCESS,
                PaymentDate = DateTime.UtcNow
            };

            if (policyId.HasValue)
            {
                var policy = await _context.Policies
                                    .Include(p => p.Customer)
                                    .Include(p => p.PolicyOffering)
                                    .FirstOrDefaultAsync(p => p.PolicyId == policyId.Value);
                if (policy != null)
                {
                    viewModel.PolicyId = policy.PolicyId;
                    viewModel.PolicyNumber = policy.PolicyNumber;
                    viewModel.CustomerName = policy.Customer?.UserName;
                    viewModel.OfferingName = policy.PolicyOffering?.OfferingName;
                    viewModel.Amount = policy.ActualPremiumAmount;
                }
                else
                {
                    TempData["ErrorMessage"] = "Selected Policy not found.";
                }
            }
            ViewBag.PolicyIdList = new SelectList(await _context.Policies.Where(p => p.PolicyStatus != PolicyStatus.EXPIRED && p.PolicyStatus != PolicyStatus.CANCELLED).OrderBy(p => p.PolicyNumber).ToListAsync(), "PolicyId", "PolicyNumber", policyId);
            return View(viewModel);
        }

        // POST: Payments/RecordManualPayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RecordManualPayment(PaymentViewModel paymentViewModel)
        {
            var policy = await _context.Policies.FindAsync(paymentViewModel.PolicyId);
            if (policy == null)
            {
                ModelState.AddModelError("PolicyId", "Selected policy not found.");
            }

            if (ModelState.IsValid)
            {
                var payment = new Payment
                {
                    PolicyId = paymentViewModel.PolicyId,
                    Amount = paymentViewModel.Amount,
                    PaymentDate = paymentViewModel.PaymentDate,
                    Method = paymentViewModel.Method,
                    Status = paymentViewModel.Status,
                    TransactionId = paymentViewModel.TransactionId,
                    Notes = paymentViewModel.Notes
                };
                _context.Payments.Add(payment);

                if (payment.Status == PaymentStatus.SUCCESS && policy != null && policy.PolicyStatus == PolicyStatus.PENDING_PAYMENT)
                {
                    policy.PolicyStatus = PolicyStatus.ACTIVE;
                    _context.Update(policy);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Manual payment recorded successfully.";
                return RedirectToAction(nameof(Index));
            }
            ViewBag.PolicyIdList = new SelectList(await _context.Policies.Where(p => p.PolicyStatus != PolicyStatus.EXPIRED && p.PolicyStatus != PolicyStatus.CANCELLED).OrderBy(p => p.PolicyNumber).ToListAsync(), "PolicyId", "PolicyNumber", paymentViewModel.PolicyId);
            if (policy != null)
            {
                paymentViewModel.PolicyNumber = policy.PolicyNumber;
                var customer = await _context.Users.FindAsync(policy.CustomerId);
                paymentViewModel.CustomerName = customer?.UserName;
                var offering = await _context.PolicyOfferings.FindAsync(policy.PolicyOfferingId);
                paymentViewModel.OfferingName = offering?.OfferingName;
            }
            return View(paymentViewModel);
        }


        // GET: Payments/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var payment = await _context.Payments
                .Include(p => p.Policy).ThenInclude(pol => pol!.Customer)
                .Include(p => p.Policy).ThenInclude(pol => pol!.PolicyOffering) // Include offering
                .FirstOrDefaultAsync(p => p.PaymentId == id);
            if (payment == null) return NotFound();

            var viewModel = new PaymentViewModel
            {
                PaymentId = payment.PaymentId,
                PolicyId = payment.PolicyId,
                PolicyNumber = payment.Policy?.PolicyNumber,
                CustomerName = payment.Policy?.Customer?.UserName,
                OfferingName = payment.Policy?.PolicyOffering?.OfferingName,
                Amount = payment.Amount,
                PaymentDate = payment.PaymentDate,
                Method = payment.Method,
                Status = payment.Status,
                TransactionId = payment.TransactionId,
                Notes = payment.Notes
            };
            return View(viewModel);
        }

        // POST: Payments/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, PaymentViewModel paymentViewModel)
        {
            if (id != paymentViewModel.PaymentId) return NotFound();

            if (ModelState.IsValid)
            {
                var paymentToUpdate = await _context.Payments.FindAsync(id);
                if (paymentToUpdate == null) return NotFound();

                paymentToUpdate.Amount = paymentViewModel.Amount;
                paymentToUpdate.PaymentDate = paymentViewModel.PaymentDate;
                paymentToUpdate.Method = paymentViewModel.Method;
                paymentToUpdate.Status = paymentViewModel.Status;
                paymentToUpdate.TransactionId = paymentViewModel.TransactionId;
                paymentToUpdate.Notes = paymentViewModel.Notes;

                try
                {
                    _context.Update(paymentToUpdate);
                    if (paymentToUpdate.Status == PaymentStatus.SUCCESS)
                    {
                        var policy = await _context.Policies.FindAsync(paymentToUpdate.PolicyId);
                        if (policy != null && policy.PolicyStatus == PolicyStatus.PENDING_PAYMENT)
                        {
                            policy.PolicyStatus = PolicyStatus.ACTIVE;
                            _context.Update(policy);
                        }
                    }
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Payment details updated successfully.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Payments.Any(e => e.PaymentId == paymentViewModel.PaymentId)) return NotFound();
                    TempData["ErrorMessage"] = "Update failed. The record was modified by another user.";
                }
                return RedirectToAction(nameof(Index));
            }
            var paymentForView = await _context.Payments
               .Include(p => p.Policy).ThenInclude(pol => pol!.Customer)
               .Include(p => p.Policy).ThenInclude(pol => pol!.PolicyOffering)
               .FirstOrDefaultAsync(p => p.PaymentId == id);
            if (paymentForView != null)
            {
                paymentViewModel.PolicyNumber = paymentForView.Policy?.PolicyNumber;
                paymentViewModel.CustomerName = paymentForView.Policy?.Customer?.UserName;
                paymentViewModel.OfferingName = paymentForView.Policy?.PolicyOffering?.OfferingName;
            }
            return View(paymentViewModel);
        }

        // GET: Payments/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var payment = await _context.Payments
                .Include(p => p.Policy).ThenInclude(pol => pol!.Customer)
                .FirstOrDefaultAsync(m => m.PaymentId == id);
            if (payment == null) return NotFound();
            return View(payment);
        }

        // POST: Payments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Payment record deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Payment record not found.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}