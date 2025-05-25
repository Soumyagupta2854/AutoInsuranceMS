using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AutoInsuranceManagementSystem.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AutoInsuranceManagementSystem.ViewModels
{
    public class EditUserRoleViewModel
    {
        public int UserId { get; set; }
        public string? UserName { get; set; } // For display
        public string? Email { get; set; } // For display
        public string? FullName { get; set; } // For display

        [Display(Name = "Current Role")]
        public string? CurrentCustomRoleName { get; set; } // Display current custom role

        [Required(ErrorMessage = "Please select a new role.")]
        [Display(Name = "New Role")]
        public UserRole NewCustomRole { get; set; } // The custom enum role to set
        public SelectList? AvailableRoles { get; set; }
    }
}