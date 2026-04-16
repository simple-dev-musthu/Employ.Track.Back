using System.ComponentModel.DataAnnotations;

namespace EmployeeTracker.API.Models
{
    public class Employee
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Mobile { get; set; } = string.Empty;

        [MaxLength(100)]
        public string JobRole { get; set; } = string.Empty;

        public string Role { get; set; } = "Employee";

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? ProfilePhoto { get; set; }

        [MaxLength(100)]
        public string? DeviceId { get; set; }

        [MaxLength(100)]
        public string? DeviceName { get; set; }

        public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
        public ICollection<LocationLog> LocationLogs { get; set; } = new List<LocationLog>();
        public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    }
}
