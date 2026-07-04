using Microsoft.EntityFrameworkCore;
using TaskTrackerAPI.Models;

namespace TaskTrackerAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // All 5 Tables
        public DbSet<User> Users { get; set; }
        public DbSet<PersonalTask> Tasks { get; set; }
        public DbSet<Organization> Organizations { get; set; }
        public DbSet<OrganizationEmployee> OrganizationEmployees { get; set; }
        public DbSet<OrganizationTask> OrganizationTasks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ==================
            // USER
            // ==================

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<User>()
                .Property(u => u.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // ==================
            // PERSONAL TASK
            // ==================

            // ✅ FIX 1 — force table name to PersonalTasks
            modelBuilder.Entity<PersonalTask>()
                .ToTable("PersonalTasks");

            modelBuilder.Entity<PersonalTask>()
                .Property(t => t.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<PersonalTask>()
                .HasOne(t => t.User)
                .WithMany(u => u.Tasks)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // ==================
            // ORGANIZATION
            // ==================

            modelBuilder.Entity<Organization>()
                .HasIndex(o => o.CompanyEmail)
                .IsUnique();

            modelBuilder.Entity<Organization>()
                .Property(o => o.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            // ✅ FIX 2 — add WithMany(u => u.Organizations)
            modelBuilder.Entity<Organization>()
                .HasOne(o => o.AdminUser)
                .WithMany(u => u.Organizations)
                .HasForeignKey(o => o.AdminUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ==================
            // ORGANIZATION EMPLOYEE
            // ==================

            modelBuilder.Entity<OrganizationEmployee>()
                .HasIndex(e => e.Email)
                .IsUnique();

            modelBuilder.Entity<OrganizationEmployee>()
                .Property(e => e.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<OrganizationEmployee>()
                .HasOne(e => e.Organization)
                .WithMany(o => o.Employees)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            // ==================
            // ORGANIZATION TASK
            // ==================

            modelBuilder.Entity<OrganizationTask>()
                .Property(t => t.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");

            modelBuilder.Entity<OrganizationTask>()
                .HasOne(t => t.Organization)
                .WithMany(o => o.OrganizationTasks)
                .HasForeignKey(t => t.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrganizationTask>()
                .HasOne(t => t.CreatedByAdmin)
                .WithMany()
                .HasForeignKey(t => t.CreatedByAdminId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrganizationTask>()
                .HasOne(t => t.AssignedEmployee)
                .WithMany(e => e.AssignedTasks)
                .HasForeignKey(t => t.AssignedEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}