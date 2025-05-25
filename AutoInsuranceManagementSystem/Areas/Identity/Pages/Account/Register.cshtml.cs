using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using AutoInsuranceManagementSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AutoInsuranceManagementSystem.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        // private readonly IEmailSender _emailSender; 
        private readonly RoleManager<IdentityRole<int>> _roleManager;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            // IEmailSender emailSender, 
            RoleManager<IdentityRole<int>> roleManager)
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            // _emailSender = emailSender; 
            _roleManager = roleManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }
        public string ReturnUrl { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            [Required(ErrorMessage = "Full name is required.")]
            [StringLength(100)]
            [Display(Name = "Full Name")]
            public string FullName { get; set; }

            [Phone]
            [Display(Name = "Phone Number")]
            [Required(ErrorMessage = "Phone number is required.")]
            public string PhoneNumber { get; set; }

            [Display(Name = "Gender")]
            public Gender? Gender { get; set; }

            [DataType(DataType.Date)]
            [Display(Name = "Date of Birth")]
            public DateTime? DateOfBirth { get; set; }

            [Range(0, 10)]
            [Display(Name = "Number of Vehicles Owned")]
            public int? NumberOfVehicles { get; set; }

            [StringLength(100)]
            [Display(Name = "Primary Vehicle Type")]
            public string? VehicleType { get; set; }

            [StringLength(20)]
            [Display(Name = "Primary Vehicle Number (License Plate)")]
            public string? VehicleNumber { get; set; }

            [Required(ErrorMessage = "Please select a role for registration.")]
            [Display(Name = "Register As")]
            public UserRole SelectedRole { get; set; } = UserRole.CUSTOMER;
        }


        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                var user = CreateUser();

                user.FullName = Input.FullName;
                user.Gender = Input.Gender;
                user.DateOfBirth = Input.DateOfBirth;
                user.NumberOfVehicles = Input.NumberOfVehicles;
                user.VehicleType = Input.VehicleType;
                user.VehicleNumber = Input.VehicleNumber;
                user.Role = Input.SelectedRole;

                // Set properties managed by Identity directly on the user object BEFORE CreateAsync
                user.UserName = Input.Email; // UserManager.CreateAsync will use this
                user.Email = Input.Email;    // UserManager.CreateAsync will use this
                user.PhoneNumber = Input.PhoneNumber; // Set directly

                // The calls to _userStore and _emailStore for setting username/email
                // before CreateAsync are usually handled internally by UserManager.CreateAsync
                // based on the user object's properties.
                // However, explicitly setting them via the store is also a valid pattern if needed for specific store implementations.
                // For simplicity and standard Identity flow, direct assignment to user object is often sufficient before CreateAsync.
                // await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                // await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                // The problematic call to SetPhoneNumberAsync is removed.
                // PhoneNumber is set directly on the 'user' object above.

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation($"User created a new account with password and selected role: {Input.SelectedRole}.");

                    string roleName = Input.SelectedRole.ToString();

                    if (!await _roleManager.RoleExistsAsync(roleName))
                    {
                        await _roleManager.CreateAsync(new IdentityRole<int>(roleName));
                        _logger.LogWarning($"Role '{roleName}' did not exist and was created during registration. Ensure roles are pre-seeded.");
                    }
                    await _userManager.AddToRoleAsync(user, roleName);
                    _logger.LogInformation($"User '{user.UserName}' was assigned to Identity role '{roleName}'.");

                    var userId = await _userManager.GetUserIdAsync(user);
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                        protocol: Request.Scheme);
                    _logger.LogInformation($"Generated email confirmation URL (if email sending is implemented): {callbackUrl}");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email, returnUrl = returnUrl });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try { return Activator.CreateInstance<ApplicationUser>(); }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}