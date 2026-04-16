using System.ComponentModel.DataAnnotations;

namespace EmployeeTracker.API.Models
{
    public class PasswordResetOtp
    {
        public int Id { get; set; }

        [MaxLength(150)]
        public string? Email { get; set; }

        [MaxLength(20)]
        public string? Mobile { get; set; }

        [Required]
        [MaxLength(6)]
        public string OtpCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Type { get; set; } = string.Empty; // "Email" or "Mobile"

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = string.Empty; // "HR" or "Employee"

        public bool IsVerified { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(10);
    }
}
