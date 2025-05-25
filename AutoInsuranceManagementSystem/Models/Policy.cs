using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoInsuranceManagementSystem.Models
{
    public enum PolicyStatus
    {
        [Display(Name = "Pending Payment")]
        PENDING_PAYMENT,
        ACTIVE,
        INACTIVE,
        EXPIRED,
        CANCELLED
    }

    public class Policy
    {
        [Key]
        public int PolicyId { get; set; }

        [Required(ErrorMessage = "Policy number is required.")]
        [StringLength(50)]
        [Display(Name = "Policy Number")]
        public string PolicyNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Policy offering is required.")]
        [Display(Name = "Based on Offering")]
        public int PolicyOfferingId { get; set; }
        [ForeignKey("PolicyOfferingId")]
        public virtual PolicyOffering? PolicyOffering { get; set; }

        [Required(ErrorMessage = "Customer is required for this policy.")]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public virtual ApplicationUser? Customer { get; set; }

        [Display(Name = "Vehicle Details (Specific)")]
        public string? VehicleDetails { get; set; }

        [Required(ErrorMessage = "Actual coverage amount is required.")]
        [Column(TypeName = "decimal(10, 2)")]
        [Display(Name = "Actual Coverage Amount")]
        public decimal ActualCoverageAmount { get; set; }

        [Required(ErrorMessage = "Actual premium amount is required.")]
        [Column(TypeName = "decimal(10, 2)")]
        [Display(Name = "Actual Premium Amount")]
        public decimal ActualPremiumAmount { get; set; }

        [Required(ErrorMessage = "Start date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [Required]
        [Display(Name = "Policy Status")]
        public PolicyStatus PolicyStatus { get; set; }

        public virtual ICollection<Claim> Claims { get; set; }
        public virtual ICollection<Payment> Payments { get; set; }

        public Policy()
        {
            Claims = new HashSet<Claim>();
            Payments = new HashSet<Payment>();
            PolicyStatus = PolicyStatus.PENDING_PAYMENT;
        }
    }
}