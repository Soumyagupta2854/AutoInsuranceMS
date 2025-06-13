using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace AutoInsuranceManagementSystem.Models
{
    public enum UserRole
    {
        CUSTOMER, // Default role
        AGENT,
        ADMIN
    }

    public class ApplicationUser : IdentityUser<int>
    {
        // Custom Role property (persisted to DB)
        [Required]
        public UserRole Role { get; set; } = UserRole.CUSTOMER; // Default role is CUSTOMER

        [StringLength(100)]
        [Display(Name = "Full Name")]
        [Required(ErrorMessage = "Full name is required.")]
        public string FullName { get; set; }

        // PhoneNumber is already part of IdentityUser, ensure it's validated

        [Required(ErrorMessage = "Pincode is required.")]
        [StringLength(6, ErrorMessage = "Pincode must be exactly 6 digits.", MinimumLength = 6)]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Pincode must be numeric and 6 digits long.")]
        [Display(Name = "Pincode")]
        public string Pincode { get; set; }

        // Navigation properties
        [InverseProperty("Adjuster")]
        public virtual ICollection<Claim> HandledClaims { get; set; }

        [InverseProperty("User")]
        public virtual ICollection<SupportTicket> SubmittedSupportTickets { get; set; }

        [InverseProperty("AssignedAgent")]
        public virtual ICollection<SupportTicket> HandledSupportTickets { get; set; }

        [InverseProperty("Customer")]
        public virtual ICollection<Policy> Policies { get; set; }

        public ApplicationUser()
        {
            HandledClaims = new HashSet<Claim>();
            SubmittedSupportTickets = new HashSet<SupportTicket>();
            HandledSupportTickets = new HashSet<SupportTicket>();
            Policies = new HashSet<Policy>();
        }
    }
}