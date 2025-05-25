using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoInsuranceManagementSystem.Models
{
    public class PolicyOffering
    {
        [Key]
        public int PolicyOfferingId { get; set; }

        [Required(ErrorMessage = "Offering name is required.")]
        [StringLength(100)]
        [Display(Name = "Offering Name")]
        public string OfferingName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Coverage amount is required.")]
        [Column(TypeName = "decimal(10, 2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Coverage amount must be greater than 0.")]
        [Display(Name = "Coverage Amount")]
        public decimal CoverageAmount { get; set; }

        [Required(ErrorMessage = "Coverage type is required.")]
        [StringLength(100)]
        [Display(Name = "Coverage Type")]
        public string CoverageType { get; set; } = string.Empty;

        [Required(ErrorMessage = "Premium amount is required.")]
        [Column(TypeName = "decimal(10, 2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Premium amount must be greater than 0.")]
        [Display(Name = "Base Premium Amount")]
        public decimal PremiumAmount { get; set; }

        [Required(ErrorMessage = "Policy duration is required.")]
        [Range(1, 120, ErrorMessage = "Duration must be between 1 and 120 months.")]
        [Display(Name = "Duration (Months)")]
        public int DurationInMonths { get; set; }

        [Display(Name = "Is Active?")]
        public bool IsActive { get; set; } = true;

        public virtual ICollection<Policy> Policies { get; set; }

        public PolicyOffering()
        {
            Policies = new HashSet<Policy>();
        }
    }
}