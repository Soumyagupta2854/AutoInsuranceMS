using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AutoInsuranceManagementSystem.Models;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace AutoInsuranceManagementSystem.ViewModels
{
    public class SupportTicketCreateViewModel
    {
        [Required(ErrorMessage = "A subject for the ticket is required.")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Subject must be between 5 and 200 characters.")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Issue description is required.")]
        [StringLength(4000, MinimumLength = 10, ErrorMessage = "Issue description must be between 10 and 4000 characters.")]
        [Display(Name = "Describe Your Issue")]
        public string IssueDescription { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Priority")]
        public TicketPriority Priority { get; set; } = TicketPriority.MEDIUM;

        [Display(Name = "Related Policy (Optional)")]
        public int? RelatedPolicyId { get; set; }
        public SelectList? AvailablePolicies { get; set; }
    }
}