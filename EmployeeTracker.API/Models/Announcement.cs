using System.ComponentModel.DataAnnotations;

namespace EmployeeTracker.API.Models
{
    public class Announcement
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Body { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Priority { get; set; } = "Low"; // High, Medium, Low

        public int PostedByEmployeeId { get; set; }
        public Employee? PostedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<AnnouncementRead> Reads { get; set; } = new List<AnnouncementRead>();
    }
}
