using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // For Column attribute
using AutoInsuranceManagementSystem.Models;

namespace AutoInsuranceManagementSystem.ViewModels
{
    public class PaymentViewModel
    {
        public int PaymentId { get; set; }

        [Required]
        public int PolicyId { get; set; }
        public string? PolicyNumber { get; set; }
        public string? CustomerName { get; set; }
        public string? OfferingName { get; set; }


        [Required(ErrorMessage = "Payment amount is required.")]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(10, 2)")]
        [Display(Name = "Amount to Pay")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "Payment method is required.")]
        [Display(Name = "Payment Method")]
        public PaymentMethod Method { get; set; }

        [Display(Name = "Card Number (if applicable)")]
        [CreditCard(ErrorMessage = "Invalid card number.")]
        [StringLength(19, MinimumLength = 13)]
        public string? CardNumber { get; set; }

        [Display(Name = "Expiry Date (MM/YY)")]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/?([0-9]{2})$", ErrorMessage = "Invalid expiry date format (MM/YY).")]
        public string? ExpiryDate { get; set; }

        [Display(Name = "CVV (if applicable)")]
        [StringLength(4, MinimumLength = 3)]
        public string? CVV { get; set; }

        [Display(Name = "Payment Date")]
        [DataType(DataType.Date)]
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        [Display(Name = "Payment Status")]
        public PaymentStatus Status { get; set; } = PaymentStatus.PENDING;

        [StringLength(255)]
        [Display(Name = "Transaction ID / Reference")]
        public string? TransactionId { get; set; }

        [StringLength(500)]
        [Display(Name = "Notes (for Admin)")]
        public string? Notes { get; set; }
    }
}