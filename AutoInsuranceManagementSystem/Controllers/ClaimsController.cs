// --- File: Controllers/ClaimsController.cs ---
using System;
using System.IO;
using System.Linq;
using System.Security.Claims; // This 'Claim' is from the security system
using System.Threading.Tasks;
using AutoInsuranceManagementSystem.Data;
// Also, explicitly qualify ClaimStatus to avoid potential ambiguity if other enums have similar names
using AutoInsuranceManagementSystem.Models;
// using AutoInsuranceManagementSystem.Models; // Can be commented out or kept if other models are used directly
using AutoInsuranceManagementSystem.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore; // << इंश्योर करें कि यह यूजिंग डायरेक्टिव मौजूद है (Ensure this using directive is present)
// Add the alias here for your custom Claim model
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
            var claims = await _context.Claims
                                .Include(c => c.Policy).ThenInclude(p => p!.Customer)
                                .Include(c => c.Policy).ThenInclude(p => p!.PolicyOffering)
                                .Include(c => c.Adjuster)
                                .OrderByDescending(c => c.DateSubmitted)
                                .ToListAsync();
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

            if (User.IsInRole("Customer"))
            {
                var currentUserId = _userManager.GetUserId(User);
                if (claim.Policy == null || claim.Policy.CustomerId.ToString() != currentUserId)
                {
                    return Forbid();
                }
            }
            return View(claim);
        }

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
               .OrderBy(u => u.UserName)
               .ToListAsync();
            ViewBag.AdjusterId = new SelectList(potentialAdjusters, "Id", "UserName", selectedAdjuster);
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
                var policy = await _context.Policies.FindAsync(viewModel.PolicyId);
                if (policy == null || (policy.PolicyStatus != PolicyStatus.ACTIVE && policy.PolicyStatus != PolicyStatus.PENDING_PAYMENT))
                {
                    ModelState.AddModelError("PolicyId", "Selected policy is not valid or not active for claims.");
                }
                else if (viewModel.ClaimDate < policy.StartDate || viewModel.ClaimDate > policy.EndDate.AddDays(7))
                {
                    ModelState.AddModelError("ClaimDate", $"Incident date must be within or shortly after the policy period ({policy.StartDate:yyyy-MM-dd} - {policy.EndDate:yyyy-MM-dd}).");
                }

                if (User.IsInRole("Customer") && policy != null && policy.CustomerId != currentUser.Id)
                {
                    ModelState.AddModelError("PolicyId", "You can only submit claims for your own policies.");
                }

                string? photoPath = null;
                string? videoPath = null;

                if (viewModel.PhotoProof != null)
                {
                    photoPath = await SaveUploadedFile(viewModel.PhotoProof, "photos");
                    if (photoPath == null) ModelState.AddModelError("PhotoProof", "Invalid photo file type or error saving file.");
                }

                if (viewModel.VideoProof != null)
                {
                    videoPath = await SaveUploadedFile(viewModel.VideoProof, "videos");
                    if (videoPath == null) ModelState.AddModelError("VideoProof", "Invalid video file type or error saving file.");
                }


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
                    await _context.SaveChangesAsync(); // This is approx line 195 (or 198 if comments are counted differently)

                    if (!string.IsNullOrEmpty(photoPath) && claim.ClaimId > 0)
                    {
                        var finalPhotoPath = await MoveToClaimSpecificFolder(photoPath, claim.ClaimId, "photos");
                        if (finalPhotoPath != null) claim.PhotoProofPath = finalPhotoPath; else claim.PhotoProofPath = null;
                    }
                    if (!string.IsNullOrEmpty(videoPath) && claim.ClaimId > 0)
                    {
                        var finalVideoPath = await MoveToClaimSpecificFolder(videoPath, claim.ClaimId, "videos");
                        if (finalVideoPath != null) claim.VideoProofPath = finalVideoPath; else claim.VideoProofPath = null;
                    }

                    if (claim.PhotoProofPath != photoPath || claim.VideoProofPath != videoPath)
                    {
                        _context.Update(claim);
                        await _context.SaveChangesAsync();
                    }

                    TempData["SuccessMessage"] = "Claim submitted successfully with proofs!";
                    if (User.IsInRole("Customer")) return RedirectToAction(nameof(MyClaims));
                    return RedirectToAction(nameof(Index));
                }
            }
            if (User.IsInRole("Customer"))
            {
                await PopulatePoliciesDropDownListForClaim(currentUser.Id, viewModel.PolicyId);
            }
            else
            {
                await PopulatePoliciesDropDownListForClaim(selectedPolicy: viewModel.PolicyId);
            }
            return View(viewModel);
        }

        private async Task<string?> SaveUploadedFile(IFormFile file, string subfolder)
        {
            if (file == null || file.Length == 0) return null;

            var allowedPhotoExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var allowedVideoExtensions = new[] { ".mp4", ".mov", ".avi", ".wmv", ".mkv" };

            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

            if (subfolder == "photos" && !allowedPhotoExtensions.Contains(fileExtension)) return null;
            if (subfolder == "videos" && !allowedVideoExtensions.Contains(fileExtension)) return null;

            var tempUploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "temp_claim_proofs", subfolder);
            if (!Directory.Exists(tempUploadsFolder)) Directory.CreateDirectory(tempUploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
            var tempFilePath = Path.Combine(tempUploadsFolder, uniqueFileName);

            try
            {
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }
                return Path.Combine("uploads", "temp_claim_proofs", subfolder, uniqueFileName).Replace(Path.DirectorySeparatorChar, '/');
            }
            catch (Exception ex)
            {
                // Log error (ex.ToString())
                return null;
            }
        }

        private async Task<string?> MoveToClaimSpecificFolder(string tempRelativePath, int claimId, string subfolder)
        {
            if (string.IsNullOrEmpty(tempRelativePath)) return null;

            var webRootPath = _webHostEnvironment.WebRootPath;
            var tempFullPath = Path.Combine(webRootPath, tempRelativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

            if (!System.IO.File.Exists(tempFullPath)) return null;

            var claimSpecificFolderName = $"claim_{claimId}";
            var finalUploadsFolder = Path.Combine(webRootPath, "uploads", "claim_proofs", claimSpecificFolderName, subfolder);
            if (!Directory.Exists(finalUploadsFolder)) Directory.CreateDirectory(finalUploadsFolder);

            var fileName = Path.GetFileName(tempFullPath);
            var finalFilePath = Path.Combine(finalUploadsFolder, fileName);
            var finalRelativePath = Path.Combine("uploads", "claim_proofs", claimSpecificFolderName, subfolder, fileName).Replace(Path.DirectorySeparatorChar, '/');

            try
            {
                System.IO.File.Move(tempFullPath, finalFilePath);
                return finalRelativePath;
            }
            catch (Exception ex)
            {
                // Log error (ex.ToString())
                try { System.IO.File.Delete(tempFullPath); } catch { /* ignore delete error */ }
                return null;
            }
        }

        [Authorize(Roles = "Admin,Agent")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var claim = await _context.Claims.Include(c => c.Policy).FirstOrDefaultAsync(c => c.ClaimId == id);
            if (claim == null) return NotFound();

            await PopulateAdjustersDropDownList(claim.AdjusterId);
            return View(claim);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Agent")]
        public async Task<IActionResult> Edit(int id, [Bind("ClaimId,PolicyId,ClaimAmount,ClaimDate,ClaimStatus,AdjusterId,Description,PhotoProofPath,VideoProofPath")] InsuranceClaim claim)
        {
            if (id != claim.ClaimId) return NotFound();

            if (ModelState.IsValid)
            {
                var originalPolicy = await _context.Policies.AsNoTracking().FirstOrDefaultAsync(p => p.PolicyId == claim.PolicyId);
                if (originalPolicy == null)
                {
                    ModelState.AddModelError("PolicyId", "Associated policy is not valid.");
                }
                else if (claim.ClaimDate < originalPolicy.StartDate || claim.ClaimDate > originalPolicy.EndDate.AddDays(7))
                {
                    ModelState.AddModelError("ClaimDate", $"Incident date must be within or shortly after the policy period ({originalPolicy.StartDate:yyyy-MM-dd} - {originalPolicy.EndDate:yyyy-MM-dd}).");
                }

                if (ModelState.IsValid)
                {
                    try
                    {
                        var existingClaim = await _context.Claims.AsNoTracking().FirstOrDefaultAsync(c => c.ClaimId == id);
                        if (existingClaim != null)
                        {
                            if (string.IsNullOrEmpty(claim.PhotoProofPath)) claim.PhotoProofPath = existingClaim.PhotoProofPath;
                            if (string.IsNullOrEmpty(claim.VideoProofPath)) claim.VideoProofPath = existingClaim.VideoProofPath;
                        }
                        claim.DateSubmitted = existingClaim?.DateSubmitted ?? DateTime.UtcNow;

                        _context.Update(claim);
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = "Claim updated successfully!";
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!ClaimExists(claim.ClaimId)) return NotFound();
                        TempData["ErrorMessage"] = "Could not update claim. The record was modified by another user.";
                    }
                    return RedirectToAction(nameof(Index));
                }
            }
            await PopulateAdjustersDropDownList(claim.AdjusterId);
            var claimWithError = await _context.Claims.Include(c => c.Policy).FirstOrDefaultAsync(c => c.ClaimId == claim.ClaimId);
            return View(claimWithError ?? claim);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var claim = await _context.Claims
                                .Include(c => c.Policy)
                                .Include(c => c.Adjuster)
                                .FirstOrDefaultAsync(m => m.ClaimId == id);
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
                if (!string.IsNullOrEmpty(claim.PhotoProofPath)) DeleteFileFromServer(claim.PhotoProofPath);
                if (!string.IsNullOrEmpty(claim.VideoProofPath)) DeleteFileFromServer(claim.VideoProofPath);

                var claimSpecificFolderName = $"claim_{claim.ClaimId}";
                var claimFolderPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "claim_proofs", claimSpecificFolderName);
                try
                {
                    if (Directory.Exists(claimFolderPath))
                    {
                        if (!Directory.EnumerateFileSystemEntries(claimFolderPath).Any())
                        {
                            Directory.Delete(claimFolderPath);
                        }
                    }
                }
                catch (Exception ex) { /* Log deletion error of folder: ex.ToString() */ }


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

        private void DeleteFileFromServer(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return;
            var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            try
            {
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                // Log the error (ex.ToString())
            }
        }

        private bool ClaimExists(int id) => _context.Claims.Any(e => e.ClaimId == id);
    }
}
