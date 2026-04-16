using System.ComponentModel.DataAnnotations.Schema;

namespace EmployeeTracker.API.Models
{
    public class LocationLog
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }

        [ForeignKey("EmployeeId")]
        public Employee Employee { get; set; } = null!;

        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
    }
}