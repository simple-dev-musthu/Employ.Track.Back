using Microsoft.EntityFrameworkCore;
using EmployeeTracker.API.Models;

namespace EmployeeTracker.API.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Employee> Employees => Set<Employee>();
        public DbSet<Attendance> Attendances => Set<Attendance>();
        public DbSet<LocationLog> LocationLogs => Set<LocationLog>();
        public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
        public DbSet<PasswordResetOtp> PasswordResetOtps => Set<PasswordResetOtp>();
        public DbSet<Announcement> Announcements => Set<Announcement>();
        public DbSet<AnnouncementRead> AnnouncementReads => Set<AnnouncementRead>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Employee>()
                .HasIndex(e => e.Email)
                .IsUnique();

            modelBuilder.Entity<Attendance>()
                .HasIndex(a => new { a.EmployeeId, a.Date });

            modelBuilder.Entity<LocationLog>()
                .HasIndex(l => new { l.EmployeeId, l.IsActive });

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(l => l.ApprovedBy)
                .WithMany()
                .HasForeignKey(l => l.ApprovedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<LeaveRequest>()
                .HasOne(l => l.Employee)
                .WithMany(e => e.LeaveRequests)
                .HasForeignKey(l => l.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Announcement>()
                .HasOne(a => a.PostedBy)
                .WithMany()
                .HasForeignKey(a => a.PostedByEmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AnnouncementRead>()
                .HasOne(r => r.Announcement)
                .WithMany(a => a.Reads)
                .HasForeignKey(r => r.AnnouncementId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AnnouncementRead>()
                .HasOne(r => r.Employee)
                .WithMany()
                .HasForeignKey(r => r.EmployeeId)
                .OnDelete(DeleteBehavior.Cascade);
                
            modelBuilder.Entity<AnnouncementRead>()
                .HasIndex(r => new { r.AnnouncementId, r.EmployeeId })
                .IsUnique();

            modelBuilder.Entity<Employee>().HasData(new Employee
            {
                Id = 1,
                Name = "HR Admin",
                Email = "hr@company.com",
                PasswordHash = "$2a$11$zez6SM3KqRoWiGLFQFHLROeAnEDmRYrSBMiKoIFRFaLWFGhGOSVAy",
                Mobile = "0000000000",
                JobRole = "HR Manager",
                Role = "HR",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        }
    }
}
