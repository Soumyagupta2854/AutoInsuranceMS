using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // For Column
using AutoInsuranceManagementSystem.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AutoInsuranceManagementSystem.ViewModels
{
    public class ClaimCreateViewModel
    {
        [Required(ErrorMessage = "Please select the policy for this claim.")]
        [Display(Name = "Select Your Policy")]
        public int PolicyId { get; set; }
        public SelectList? AvailablePolicies { get; set; }

        [Required(ErrorMessage = "Claim amount is required.")]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(10, 2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Claim amount must be greater than 0.")]
        [Display(Name = "Estimated Claim Amount")]
        public decimal ClaimAmount { get; set; }

        [Required(ErrorMessage = "Date of incident is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Date of Incident")]
        public DateTime ClaimDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "A description of the incident is required.")]
        [StringLength(1000, MinimumLength = 20, ErrorMessage = "Description must be between 20 and 1000 characters.")]
        [Display(Name = "Incident Description")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Upload Photo Proof (e.g., damages, scene)")]
        public IFormFile? PhotoProof { get; set; }

        [Display(Name = "Upload Video Proof (Optional)")]
        public IFormFile? VideoProof { get; set; }
    }
}