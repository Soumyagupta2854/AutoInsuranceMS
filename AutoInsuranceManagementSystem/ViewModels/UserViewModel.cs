using System.ComponentModel.DataAnnotations;
using AutoInsuranceManagementSystem.Models;

namespace AutoInsuranceManagementSystem.ViewModels
{
    public class UserViewModel
    {
        public int Id { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public string? FullName { get; set; }
        [Display(Name = "Current Role")]
        public UserRole CustomRole { get; set; } // The custom enum role
        public string? CurrentIdentityRoleName { get; set; } // The string name of the ASP.NET Identity Role
    }
}