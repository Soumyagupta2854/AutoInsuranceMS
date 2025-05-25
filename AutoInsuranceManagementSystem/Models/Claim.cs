using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoInsuranceManagementSystem.Models
{
    public enum ClaimStatus
    {
        [Display(Name = "Open")] OPEN,
        [Display(Name = "Under Review")] UNDER_REVIEW,
        [Display(Name = "Approved")] APPROVED,
        [Display(Name = "Rejected")] REJECTED,
        [Display(Name = "Closed")] CLOSED
    }

    public class Claim
    {
        [Key]
        public int ClaimId { get; set; }

        [Required(ErrorMessage = "Policy is required.")]
        [Display(Name = "Policy Number")]
        public int PolicyId { get; set; }
        [ForeignKey("PolicyId")]
        public virtual Policy? Policy { get; set; }

        [Required(ErrorMessage = "Claim amount is required.")]
        [Column(TypeName = "decimal(10, 2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Claim amount must be greater than 0.")]
        [Display(Name = "Claim Amount")]
        public decimal ClaimAmount { get; set; }

        [Required(ErrorMessage = "Date of incident is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Incident")]
        public DateTime ClaimDate { get; set; }

        [Required]
        [Display(Name = "Claim Status")]
        public ClaimStatus ClaimStatus { get; set; }

        [Display(Name = "Assigned Adjuster")]
        public int? AdjusterId { get; set; }
        [ForeignKey("AdjusterId")]
        public virtual ApplicationUser? Adjuster { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
        [Display(Name = "Incident Description")]
        public string? Description { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Date Submitted")]
        public DateTime DateSubmitted { get; set; }

        [StringLength(2048)]
        [Display(Name = "Photo Proof")]
        public string? PhotoProofPath { get; set; }

        [StringLength(2048)]
        [Display(Name = "Video Proof")]
        public string? VideoProofPath { get; set; }

        public Claim()
        {
            DateSubmitted = DateTime.UtcNow;
            ClaimStatus = ClaimStatus.OPEN;
        }
    }
}