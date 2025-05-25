using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AutoInsuranceManagementSystem.Models
{
    public enum TicketStatus
    {
        [Display(Name = "Open")] OPEN,
        [Display(Name = "In Progress")] IN_PROGRESS,
        [Display(Name = "On Hold")] ON_HOLD,
        [Display(Name = "Resolved")] RESOLVED,
        [Display(Name = "Closed")] CLOSED
    }

    public enum TicketPriority
    {
        [Display(Name = "Low")] LOW,
        [Display(Name = "Medium")] MEDIUM,
        [Display(Name = "High")] HIGH,
        [Display(Name = "Urgent")] URGENT
    }

    public class SupportTicket
    {
        [Key]
        public int TicketId { get; set; }

        [Required(ErrorMessage = "A subject for the ticket is required.")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Subject must be between 5 and 200 characters.")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Issue description is required.")]
        [StringLength(4000, MinimumLength = 10, ErrorMessage = "Issue description must be between 10 and 4000 characters.")]
        [Display(Name = "Issue Description")]
        public string IssueDescription { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Ticket Status")]
        public TicketStatus TicketStatus { get; set; }

        [Required]
        [Display(Name = "Priority")]
        public TicketPriority Priority { get; set; } = TicketPriority.MEDIUM;


        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Date Created")]
        public DateTime CreatedDate { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Last Updated")]
        public DateTime LastUpdatedDate { get; set; }


        [DataType(DataType.DateTime)]
        [Display(Name = "Date Resolved")]
        public DateTime? ResolvedDate { get; set; }

        [StringLength(4000)]
        [Display(Name = "Resolution Notes")]
        public string? ResolutionNotes { get; set; }

        // User who submitted the ticket (Customer)
        [Required]
        [Display(Name = "Submitted By")]
        public int UserId { get; set; }
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        // Agent assigned to handle the ticket
        [Display(Name = "Assigned Agent")]
        public int? AssignedAgentId { get; set; }
        [ForeignKey("AssignedAgentId")]
        public virtual ApplicationUser? AssignedAgent { get; set; }

        // Optional: Link to a specific policy if the ticket is about one
        [Display(Name = "Related Policy (Optional)")]
        public int? RelatedPolicyId { get; set; }
        [ForeignKey("RelatedPolicyId")]
        public virtual Policy? RelatedPolicy { get; set; }


        public SupportTicket()
        {
            CreatedDate = DateTime.UtcNow;
            LastUpdatedDate = DateTime.UtcNow;
            TicketStatus = TicketStatus.OPEN;
            Priority = TicketPriority.MEDIUM;
        }
    }
}