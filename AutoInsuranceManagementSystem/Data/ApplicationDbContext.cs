using AutoInsuranceManagementSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace AutoInsuranceManagementSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { } // Corrected constructor if the cast was needed, or base(options) if not.

        public DbSet<PolicyOffering> PolicyOfferings { get; set; }
        public DbSet<Policy> Policies { get; set; }
        public DbSet<Claim> Claims { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Policy>().Property(p => p.PolicyStatus).HasConversion<string>();
            modelBuilder.Entity<Policy>().HasOne(p => p.PolicyOffering).WithMany(po => po.Policies).HasForeignKey(p => p.PolicyOfferingId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Claim>().Property(c => c.ClaimStatus).HasConversion<string>();
            modelBuilder.Entity<Payment>().Property(p => p.Status).HasConversion<string>();
            modelBuilder.Entity<Payment>().Property(p => p.Method).HasConversion<string>();

            modelBuilder.Entity<SupportTicket>().Property(st => st.TicketStatus).HasConversion<string>();
            modelBuilder.Entity<SupportTicket>().Property(st => st.Priority).HasConversion<string>();

            modelBuilder.Entity<ApplicationUser>().Property(u => u.Role).HasConversion<string>();
           

            modelBuilder.Entity<Policy>().HasOne(p => p.Customer).WithMany(u => u.Policies).HasForeignKey(p => p.CustomerId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Claim>().HasOne(c => c.Policy).WithMany(p => p.Claims).HasForeignKey(c => c.PolicyId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Claim>().HasOne(c => c.Adjuster).WithMany(u => u.HandledClaims).HasForeignKey(c => c.AdjusterId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<Payment>().HasOne(p => p.Policy).WithMany(po => po.Payments).HasForeignKey(p => p.PolicyId).OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SupportTicket>()
                .HasOne(st => st.User)
                .WithMany(u => u.SubmittedSupportTickets)
                .HasForeignKey(st => st.UserId)
                .OnDelete(DeleteBehavior.Restrict); // << --- MODIFIED THIS LINE ---

            modelBuilder.Entity<SupportTicket>()
                .HasOne(st => st.AssignedAgent)
                .WithMany(u => u.HandledSupportTickets)
                .HasForeignKey(st => st.AssignedAgentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<SupportTicket>()
                .HasOne(st => st.RelatedPolicy)
                .WithMany() // Assuming a Policy doesn't have a direct navigation collection to SupportTickets here. If it does, specify it.
                .HasForeignKey(st => st.RelatedPolicyId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}