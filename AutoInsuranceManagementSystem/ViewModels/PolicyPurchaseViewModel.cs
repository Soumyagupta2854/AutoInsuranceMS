using System;
using System.ComponentModel.DataAnnotations;
using AutoInsuranceManagementSystem.Models; // Assuming CoverageType enum is in Models

namespace AutoInsuranceManagementSystem.ViewModels
{
    public class PolicyPurchaseViewModel
    {
        // --- Hidden fields to carry over offering details ---
        [Required]
        public int PolicyOfferingId { get; set; }

        [Display(Name = "Policy Offering")]
        public string OfferingName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Display(Name = "Coverage Type")]
        // If CoverageType is an enum, it's better to use the enum type directly
        // public CoverageType CoverageType { get; set; } 
        // Or keep as string if that's intended
        public string CoverageType { get; set; } = string.Empty;

        [Display(Name = "Coverage Amount")]
        [DataType(DataType.Currency)]
        public decimal CoverageAmount { get; set; }

        [Display(Name = "Premium")]
        [DataType(DataType.Currency)]
        public decimal PremiumAmount { get; set; }

        [Display(Name = "Duration")]
        public int DurationInMonths { get; set; }

        // --- User-Editable Vehicle Fields ---

        [Required(ErrorMessage = "Vehicle Registration Number is mandatory.")]
        [Display(Name = "Vehicle Registration No.")]
        // This regular expression validates a common format for Indian vehicle registration plates.
        // You can adjust this pattern for different regions.
        [RegularExpression(@"^[A-Z]{2}[ -]?[0-9]{1,2}[ -]?[A-Z]{1,2}[ -]?[0-9]{4}$", ErrorMessage = "Please enter a valid Indian vehicle registration number (e.g., TN 07 BU 1234).")]
        public string VehicleRegistrationNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the vehicle's make (e.g., Maruti Suzuki, Hyundai).")]
        [StringLength(50)]
        [Display(Name = "Vehicle Make")]
        public string VehicleMake { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the vehicle's model (e.g., Swift, Creta).")]
        [StringLength(50)]
        [Display(Name = "Vehicle Model")]
        public string VehicleModel { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the vehicle's manufacturing year.")]
        // Using a realistic range for vehicle manufacturing years.
        [Range(1980, 2025, ErrorMessage = "Please enter a valid year (1980-2025).")]
        [Display(Name = "Year of Manufacture")]
        public int VehicleYear { get; set; }

        // --- Policy Start Date ---

        [Required(ErrorMessage = "Please select a start date for your policy.")]
        [DataType(DataType.Date)]
        [Display(Name = "Desired Policy Start Date")]
        public DateTime StartDate { get; set; } = DateTime.Today;
    }
}