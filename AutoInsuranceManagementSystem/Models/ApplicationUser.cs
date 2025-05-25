using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using System; // Required for DateTime

namespace AutoInsuranceManagementSystem.Models
{
    public enum UserRole
    {
        CUSTOMER, // Default
        AGENT,
        ADMIN
    }

    public enum Gender
    {
        Male,
        Female,
        Other,
        [Display(Name = "Prefer not to say")]
        PreferNotToSay
    }

    public class ApplicationUser : IdentityUser<int>
    {
        // Custom Role property (persisted to DB)
        [Required]
        public UserRole Role { get; set; } = UserRole.CUSTOMER; // Default role is CUSTOMER

        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string? FullName { get; set; }

        // Additional properties for Customer registration
        [Display(Name = "Gender")]
        public Gender? Gender { get; set; } // Nullable if optional

        [DataType(DataType.Date)]
        [Display(Name = "Date of Birth")]
        public DateTime? DateOfBirth { get; set; } // Nullable if optional

        // PhoneNumber is already part of IdentityUser, ensure it's used/prompted for

        [Range(0, 10, ErrorMessage = "Number of vehicles must be between 0 and 10.")]
        [Display(Name = "Number of Vehicles Owned")]
        public int? NumberOfVehicles { get; set; } // Nullable if optional

        [StringLength(100)]
        [Display(Name = "Primary Vehicle Type")]
        public string? VehicleType { get; set; } // e.g., Sedan, SUV, Truck

        [StringLength(20)]
        [Display(Name = "Primary Vehicle Number")]
        public string? VehicleNumber { get; set; } // License plate


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