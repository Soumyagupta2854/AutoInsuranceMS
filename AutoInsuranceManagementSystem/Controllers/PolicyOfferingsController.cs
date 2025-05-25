using System.Linq;
using System.Threading.Tasks;
using AutoInsuranceManagementSystem.Data;
using AutoInsuranceManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoInsuranceManagementSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PolicyOfferingsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PolicyOfferingsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: PolicyOfferings
        public async Task<IActionResult> Index()
        {
            return View(await _context.PolicyOfferings.OrderBy(po => po.OfferingName).ToListAsync());
        }

        // GET: PolicyOfferings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var policyOffering = await _context.PolicyOfferings
                .FirstOrDefaultAsync(m => m.PolicyOfferingId == id);
            if (policyOffering == null)
            {
                return NotFound();
            }

            return View(policyOffering);
        }

        // GET: PolicyOfferings/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: PolicyOfferings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("OfferingName,Description,CoverageAmount,CoverageType,PremiumAmount,DurationInMonths,IsActive")] PolicyOffering policyOffering)
        {
            if (ModelState.IsValid)
            {
                _context.Add(policyOffering);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Policy offering created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(policyOffering);
        }

        // GET: PolicyOfferings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var policyOffering = await _context.PolicyOfferings.FindAsync(id);
            if (policyOffering == null)
            {
                return NotFound();
            }
            return View(policyOffering);
        }

        // POST: PolicyOfferings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("PolicyOfferingId,OfferingName,Description,CoverageAmount,CoverageType,PremiumAmount,DurationInMonths,IsActive")] PolicyOffering policyOffering)
        {
            if (id != policyOffering.PolicyOfferingId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(policyOffering);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Policy offering updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PolicyOfferingExists(policyOffering.PolicyOfferingId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Could not update. The record was modified by another user.";
                        // Log error
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(policyOffering);
        }

        // GET: PolicyOfferings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var policyOffering = await _context.PolicyOfferings
                .FirstOrDefaultAsync(m => m.PolicyOfferingId == id);
            if (policyOffering == null)
            {
                return NotFound();
            }

            return View(policyOffering);
        }

        // POST: PolicyOfferings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var policyOffering = await _context.PolicyOfferings.FindAsync(id);
            if (policyOffering != null)
            {
                var hasPolicies = await _context.Policies.AnyAsync(p => p.PolicyOfferingId == id);
                if (hasPolicies)
                {
                    TempData["ErrorMessage"] = "Cannot delete offering: active policies are based on it. Consider deactivating it instead.";
                    return RedirectToAction(nameof(Index));
                }
                _context.PolicyOfferings.Remove(policyOffering);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Policy offering deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Policy offering not found.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool PolicyOfferingExists(int id)
        {
            return _context.PolicyOfferings.Any(e => e.PolicyOfferingId == id);
        }
    }
}