using System; // For DateTime
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AutoInsuranceManagementSystem.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AutoInsuranceManagementSystem.ViewModels
{
    public class SupportTicketUpdateViewModel
    {
        public int TicketId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string IssueDescription { get; set; } = string.Empty;
        public string SubmittedByUserName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string? RelatedPolicyNumber { get; set; }


        [Required]
        [Display(Name = "Ticket Status")]
        public TicketStatus TicketStatus { get; set; }

        [Required]
        [Display(Name = "Priority")]
        public TicketPriority Priority { get; set; }

        [Display(Name = "Assigned Agent")]
        public int? AssignedAgentId { get; set; }
        public SelectList? AvailableAgents { get; set; }

        [StringLength(4000)]
        [Display(Name = "Resolution Notes / Agent Response")]
        public string? ResolutionNotes { get; set; }
    }
}