using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoInsuranceManagementSystem.Models
{
    public enum PaymentStatus { PENDING, SUCCESS, FAILED, REFUNDED }
    public enum PaymentMethod
    {
        [Display(Name = "Simulated Card")] SIMULATED_CARD,
        [Display(Name = "Simulated Bank Transfer")] SIMULATED_BANK_TRANSFER,
        [Display(Name = "Manual/Offline")] MANUAL_OFFLINE
    }

    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required(ErrorMessage = "Policy is required.")]
        [Display(Name = "Policy Number")]
        public int PolicyId { get; set; }
        [ForeignKey("PolicyId")]
        public virtual Policy? Policy { get; set; }

        [Required(ErrorMessage = "Payment amount is required.")]
        [Column(TypeName = "decimal(10, 2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Payment amount must be greater than 0.")]
        [Display(Name = "Payment Amount")]
        public decimal Amount { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Payment Date")]
        public DateTime PaymentDate { get; set; }

        [Required]
        [Display(Name = "Payment Status")]
        public PaymentStatus Status { get; set; }

        [Required(ErrorMessage = "Payment method is required.")]
        [Display(Name = "Payment Method")]
        public PaymentMethod Method { get; set; }

        [StringLength(255)]
        [Display(Name = "Transaction ID / Reference")]
        public string? TransactionId { get; set; }

        [StringLength(500)]
        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        public Payment()
        {
            PaymentDate = DateTime.UtcNow;
            Status = PaymentStatus.PENDING;
            TransactionId = Guid.NewGuid().ToString();
        }
    }
}