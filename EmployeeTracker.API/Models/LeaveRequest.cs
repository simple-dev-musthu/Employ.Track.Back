using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeTracker.API.Models
{
    public class LeaveRequest
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; } = null!;

        [Required]
        public string LeaveType { get; set; } = string.Empty;

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";

        [MaxLength(500)]
        public string? HRComment { get; set; }

        public int? ApprovedById { get; set; }

        [ForeignKey("ApprovedById")]
        public Employee? ApprovedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}