namespace EmployeeTracker.API.Models
{
    public class AnnouncementRead
    {
        public int Id { get; set; }

        public int AnnouncementId { get; set; }
        public Announcement? Announcement { get; set; }

        public int EmployeeId { get; set; }
        public Employee? Employee { get; set; }

        public DateTime ReadAt { get; set; } = DateTime.UtcNow;
    }
}
