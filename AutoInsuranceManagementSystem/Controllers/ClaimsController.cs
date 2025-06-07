using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoInsuranceManagementSystem.Data;
using AutoInsuranceManagementSystem.Models;
using AutoInsuranceManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
// Using an alias to prevent conflicts with System.Security.Claims.Claim
using InsuranceClaim = AutoInsuranceManagementSystem.Models.Claim;

namespace AutoInsuranceManagementSystem.Controllers
{
    [Authorize]
    public class ClaimsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ClaimsController(ApplicationDbContext context,
                                UserManager<ApplicationUser> userManager,
                                IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Claims (For Admin/Agent)
        [Authorize(Roles = "Admin,Agent")]
        public async Task<IActionResult> Index()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            IQueryable<InsuranceClaim> claimsQuery = _context.Claims
                .Include(c => c.Policy).ThenInclude(p => p!.Customer)
                .Include(c => c.Policy).ThenInclude(p => p!.PolicyOffering)
                .Include(c => c.Adjuster);

            // Agents only see claims assigned to them. Admins see all.
            if (User.IsInRole("Agent"))
            {
                claimsQuery = claimsQuery.Where(c => c.AdjusterId == currentUser.Id);
            }

            var claims = await claimsQuery.OrderByDescending(c => c.DateSubmitted).ToListAsync();
            return View(claims);
        }

        // GET: Claims/MyClaims (For Customer)
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> MyClaims()
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var customerClaims = await _context.Claims
                .Include(c => c.Policy).ThenInclude(p => p!.PolicyOffering)
                .Include(c => c.Adjuster)
                .Where(c => c.Policy != null && c.Policy.CustomerId.ToString() == userId)
                .OrderByDescending(c => c.DateSubmitted)
                .ToListAsync();
            return View(customerClaims);
        }

        // GET: Claims/Details/5
        [Authorize(Roles = "Admin,Agent,Customer")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var claim = await _context.Claims
                .Include(c => c.Policy).ThenInclude(p => p!.Customer)
                .Include(c => c.Policy).ThenInclude(p => p!.PolicyOffering)
                .Include(c => c.Adjuster)
                .FirstOrDefaultAsync(m => m.ClaimId == id);
            if (claim == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            // Security checks for different roles
            if (User.IsInRole("Customer") && (claim.Policy == null || claim.Policy.CustomerId != currentUser.Id)) return Forbid();
            if (User.IsInRole("Agent") && claim.AdjusterId != currentUser.Id) return Forbid();

            return View(claim);
        }

        // GET: Claims/GetPolicyDetails/5 (For JavaScript call)
        [HttpGet]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> GetPolicyDetails(int id)
        {
            var policy = await _context.Policies
                .Include(p => p.PolicyOffering)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.PolicyId == id);

            if (policy == null || policy.PolicyOffering == null)
            {
                return NotFound(new { message = "Policy or its offering details not found." });
            }

            if (User.IsInRole("Customer"))
            {
                var currentUserId = _userManager.GetUserId(User);
                if (policy.CustomerId.ToString() != currentUserId) return Forbid();
            }

            // Using Policy.StartDate as a proxy for vehicle registration date to calculate age.
            var vehicleAgeInYears = (DateTime.Today - policy.StartDate).TotalDays / 365.25;

            return Json(new
            {
                // Using the more specific ActualCoverageAmount from the Policy model itself.
                coverageAmount = policy.ActualCoverageAmount,
                // ASSUMPTION: 'HasZeroDepreciationAddOn' is not in the model. Defaulting to false.
                // To enable, add 'public bool HasZeroDepreciationAddOn { get; set; }' to your PolicyOffering.cs model.
                hasZeroDepreciation = false,
                // ASSUMPTION: 'VehicleEngineCapacity' is not in the model. Defaulting to 1499 for a ₹1,000 deductible.
                // To enable, add 'public int VehicleEngineCapacity { get; set; }' to your Policy.cs model.
                engineCC = 1499,
                vehicleAge = vehicleAgeInYears
            });
        }

        // GET: Claims/Create
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> Create(int? policyId)
        {
            var viewModel = new ClaimCreateViewModel();
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (User.IsInRole("Customer"))
            {
                await PopulatePoliciesDropDownListForClaim(currentUser.Id, policyId);
                if (policyId.HasValue) viewModel.PolicyId = policyId.Value;
            }
            else // Admin
            {
                await PopulatePoliciesDropDownListForClaim(selectedPolicy: policyId);
                if (policyId.HasValue) viewModel.PolicyId = policyId.Value;
            }
            return View(viewModel);
        }

        // POST: Claims/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Customer,Admin")]
        public async Task<IActionResult> Create(ClaimCreateViewModel viewModel)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            if (ModelState.IsValid)
            {
                var policy = await _context.Policies.Include(p => p.PolicyOffering).FirstOrDefaultAsync(p => p.PolicyId == viewModel.PolicyId);

                // --- Server-side Validation ---
                if (policy == null || policy.PolicyOffering == null)
                {
                    ModelState.AddModelError("PolicyId", "Selected policy is not valid for claims.");
                }
                else
                {
                    if (policy.PolicyStatus != PolicyStatus.ACTIVE && policy.PolicyStatus != PolicyStatus.PENDING_PAYMENT)
                        ModelState.AddModelError("PolicyId", "Claims can only be filed for ACTIVE policies.");
                    if (viewModel.ClaimDate < policy.StartDate || viewModel.ClaimDate > policy.EndDate.AddDays(7))
                        ModelState.AddModelError("ClaimDate", $"Incident date must be within or shortly after the policy period ({policy.StartDate:yyyy-MM-dd} - {policy.EndDate:yyyy-MM-dd}).");
                    if (viewModel.ClaimAmount > policy.ActualCoverageAmount)
                        ModelState.AddModelError("ClaimAmount", $"Estimated cost cannot exceed policy coverage of {policy.ActualCoverageAmount:C}.");
                    if (User.IsInRole("Customer") && policy.CustomerId != currentUser.Id)
                        ModelState.AddModelError("PolicyId", "You can only submit claims for your own policies.");
                }

                // --- File Validation ---
                string? photoPath = await SaveUploadedFile(viewModel.PhotoProof, "photos");
                if (viewModel.PhotoProof != null && photoPath == null) ModelState.AddModelError("PhotoProof", "Invalid photo file or error during save.");

                string? videoPath = await SaveUploadedFile(viewModel.VideoProof, "videos");
                if (viewModel.VideoProof != null && videoPath == null) ModelState.AddModelError("VideoProof", "Invalid video file or error during save.");

                if (ModelState.IsValid)
                {
                    var claim = new InsuranceClaim
                    {
                        PolicyId = viewModel.PolicyId,
                        ClaimAmount = viewModel.ClaimAmount,
                        ClaimDate = viewModel.ClaimDate,
                        Description = viewModel.Description,
                        PhotoProofPath = photoPath,
                        VideoProofPath = videoPath,
                        ClaimStatus = Models.ClaimStatus.OPEN,
                        DateSubmitted = DateTime.UtcNow
                    };
                    _context.Claims.Add(claim);
                    await _context.SaveChangesAsync();

                    // --- Move files to permanent folder now that we have a ClaimId ---
                    if (!string.IsNullOrEmpty(photoPath) || !string.IsNullOrEmpty(videoPath))
                    {
                        claim.PhotoProofPath = await MoveToClaimSpecificFolder(photoPath, claim.ClaimId, "photos");
                        claim.VideoProofPath = await MoveToClaimSpecificFolder(videoPath, claim.ClaimId, "videos");
                        _context.Update(claim);
                        await _context.SaveChangesAsync();
                    }

                    TempData["SuccessMessage"] = "Claim submitted successfully!";
                    return User.IsInRole("Customer") ? RedirectToAction(nameof(MyClaims)) : RedirectToAction(nameof(Index));
                }
            }

            if (User.IsInRole("Customer")) await PopulatePoliciesDropDownListForClaim(currentUser.Id, viewModel.PolicyId);
            else await PopulatePoliciesDropDownListForClaim(selectedPolicy: viewModel.PolicyId);

            return View(viewModel);
        }

        // GET: Claims/Edit/5
        [Authorize(Roles = "Admin,Agent")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var claim = await _context.Claims.Include(c => c.Policy).FirstOrDefaultAsync(c => c.ClaimId == id);
            if (claim == null) return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            if (User.IsInRole("Agent") && claim.AdjusterId != currentUser.Id)
            {
                TempData["ErrorMessage"] = "You can only edit claims assigned to you.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateAdjustersDropDownList(claim.AdjusterId);
            return View(claim);
        }

        // POST: Claims/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Agent")]
        public async Task<IActionResult> Edit(int id, [Bind("ClaimId,PolicyId,ClaimAmount,ClaimDate,ClaimStatus,AdjusterId,Description,PhotoProofPath,VideoProofPath,DateSubmitted")] InsuranceClaim claim)
        {
            if (id != claim.ClaimId) return NotFound();

            var claimToUpdate = await _context.Claims.AsNoTracking().FirstOrDefaultAsync(c => c.ClaimId == id);
            if (claimToUpdate == null) return NotFound();

            if (User.IsInRole("Agent"))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (claimToUpdate.AdjusterId != currentUser.Id)
                {
                    TempData["ErrorMessage"] = "You are not authorized to update this claim.";
                    return RedirectToAction(nameof(Index));
                }
                claim.ClaimDate = claimToUpdate.ClaimDate;
                claim.ClaimAmount = claimToUpdate.ClaimAmount;
                claim.Description = claimToUpdate.Description;
                claim.AdjusterId = claimToUpdate.AdjusterId;
                claim.PolicyId = claimToUpdate.PolicyId;
            }

            claim.DateSubmitted = claimToUpdate.DateSubmitted;
            claim.PhotoProofPath = claimToUpdate.PhotoProofPath;
            claim.VideoProofPath = claimToUpdate.VideoProofPath;

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(claim);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Claim updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClaimExists(claim.ClaimId)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            await PopulateAdjustersDropDownList(claim.AdjusterId);
            claim.Policy = await _context.Policies.AsNoTracking().FirstOrDefaultAsync(p => p.PolicyId == claim.PolicyId);
            return View(claim);
        }

        #region Helper and File Methods
        private async Task PopulatePoliciesDropDownListForClaim(int? customerIdForFilter = null, object? selectedPolicy = null)
        {
            IQueryable<Policy> policiesQuery = _context.Policies
                .Where(p => p.PolicyStatus == PolicyStatus.ACTIVE || p.PolicyStatus == PolicyStatus.PENDING_PAYMENT);
            if (customerIdForFilter.HasValue)
            {
                policiesQuery = policiesQuery.Where(p => p.CustomerId == customerIdForFilter.Value);
            }
            ViewBag.AvailablePolicies = new SelectList(await policiesQuery.OrderBy(p => p.PolicyNumber).AsNoTracking().ToListAsync(), "PolicyId", "PolicyNumber", selectedPolicy);
        }

        private async Task PopulateAdjustersDropDownList(object? selectedAdjuster = null)
        {
            var potentialAdjusters = await _userManager.Users
               .Where(u => u.Role == UserRole.AGENT || u.Role == UserRole.ADMIN)
               .OrderBy(u => u.UserName).ToListAsync();
            ViewBag.AdjusterId = new SelectList(potentialAdjusters, "Id", "UserName", selectedAdjuster);
        }

        private async Task<string?> SaveUploadedFile(IFormFile? file, string subfolder)
        {
            if (file == null || file.Length == 0) return null;
            var allowedExtensions = subfolder == "photos" ? new[] { ".jpg", ".jpeg", ".png", ".gif" } : new[] { ".mp4", ".mov", ".avi", ".wmv", ".mkv" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension)) return null;

            var tempUploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "temp_claim_proofs", subfolder);
            if (!Directory.Exists(tempUploadsFolder)) Directory.CreateDirectory(tempUploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var tempFilePath = Path.Combine(tempUploadsFolder, uniqueFileName);

            try
            {
                using var stream = new FileStream(tempFilePath, FileMode.Create);
                await file.CopyToAsync(stream);
                return Path.Combine("uploads", "temp_claim_proofs", subfolder, uniqueFileName);
            }
            catch { return null; }
        }

        private async Task<string?> MoveToClaimSpecificFolder(string? tempRelativePath, int claimId, string subfolder)
        {
            if (string.IsNullOrEmpty(tempRelativePath)) return null;
            var webRootPath = _webHostEnvironment.WebRootPath;
            var tempFullPath = Path.Combine(webRootPath, tempRelativePath.Replace('/', Path.DirectorySeparatorChar));
            if (!System.IO.File.Exists(tempFullPath)) return null;

            var claimSpecificFolderName = $"claim_{claimId}";
            var finalUploadsFolder = Path.Combine(webRootPath, "uploads", "claim_proofs", claimSpecificFolderName, subfolder);
            if (!Directory.Exists(finalUploadsFolder)) Directory.CreateDirectory(finalUploadsFolder);

            var fileName = Path.GetFileName(tempFullPath);
            var finalFilePath = Path.Combine(finalUploadsFolder, fileName);
            var finalRelativePath = Path.Combine("uploads", "claim_proofs", claimSpecificFolderName, subfolder, fileName).Replace(Path.DirectorySeparatorChar, '/');

            try
            {
                await Task.Run(() => System.IO.File.Move(tempFullPath, finalFilePath));
                return finalRelativePath;
            }
            catch
            {
                try { System.IO.File.Delete(tempFullPath); } catch { /* ignore */ }
                return null;
            }
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var claim = await _context.Claims.Include(c => c.Policy).Include(c => c.Adjuster).FirstOrDefaultAsync(m => m.ClaimId == id);
            if (claim == null) return NotFound();
            return View(claim);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var claim = await _context.Claims.FindAsync(id);
            if (claim != null)
            {
                DeleteFileFromServer(claim.PhotoProofPath);
                DeleteFileFromServer(claim.VideoProofPath);

                var claimSpecificFolderName = $"claim_{claim.ClaimId}";
                var claimFolderPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "claim_proofs", claimSpecificFolderName);
                try
                {
                    if (Directory.Exists(claimFolderPath) && !Directory.EnumerateFileSystemEntries(claimFolderPath).Any())
                    {
                        Directory.Delete(claimFolderPath, true);
                    }
                }
                catch { /* Log error */ }

                _context.Claims.Remove(claim);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Claim and associated proofs deleted successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Claim not found.";
            }
            return RedirectToAction(nameof(Index));
        }

        private void DeleteFileFromServer(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;
            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            try
            {
                if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);
            }
            catch { /* Log error */ }
        }

        private bool ClaimExists(int id) => _context.Claims.Any(e => e.ClaimId == id);
        #endregion
    }
}
