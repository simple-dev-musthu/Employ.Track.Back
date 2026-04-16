using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeTracker.API.Models
{
    public class Attendance
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; } = null!;

        public DateTime LoginTime { get; set; }
        public DateTime? LogoutTime { get; set; }

        public double LoginLatitude { get; set; }
        public double LoginLongitude { get; set; }

        public double? LogoutLatitude { get; set; }
        public double? LogoutLongitude { get; set; }

        public DateTime Date { get; set; } = DateTime.UtcNow.Date;
        public string Status { get; set; } = "Present";
    }
}