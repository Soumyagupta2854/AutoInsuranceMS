using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // For Column attribute if needed
using Microsoft.AspNetCore.Http; // For IFormFile if you were to add file uploads here
using Microsoft.AspNetCore.Mvc.Rendering; // For SelectList

namespace AutoInsuranceManagementSystem.ViewModels
{
    public class PolicyPurchaseViewModel
    {
        [Required]
        public int PolicyOfferingId { get; set; }

        [Display(Name = "Policy Offering")]
        public string OfferingName { get; set; } = string.Empty;
        public string? Description { get; set; }

        [Display(Name = "Coverage Type")]
        public string CoverageType { get; set; } = string.Empty;

        [Display(Name = "Coverage Amount")]
        [DataType(DataType.Currency)]
        public decimal CoverageAmount { get; set; }

        [Display(Name = "Premium")]
        [DataType(DataType.Currency)]
        public decimal PremiumAmount { get; set; }

        [Display(Name = "Duration")]
        public int DurationInMonths { get; set; }


        [Required(ErrorMessage = "Please enter your vehicle details.")]
        [StringLength(200, ErrorMessage = "Vehicle details cannot exceed 200 characters.")]
        [Display(Name = "Your Vehicle Details (e.g., Make, Model, Year, VIN)")]
        public string VehicleDetails { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a start date for your policy.")]
        [DataType(DataType.Date)]
        [Display(Name = "Desired Policy Start Date")]
        public DateTime StartDate { get; set; }
    }
}