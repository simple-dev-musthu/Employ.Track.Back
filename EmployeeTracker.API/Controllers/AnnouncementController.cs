using EmployeeTracker.API.Data;
using EmployeeTracker.API.DTOs;
using EmployeeTracker.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace EmployeeTracker.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AnnouncementController : ControllerBase
    {
        private readonly AppDbContext _db;

        public AnnouncementController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<AnnouncementResponseDTO>>> GetAnnouncements()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var announcements = await _db.Announcements
                .Include(a => a.PostedBy)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new AnnouncementResponseDTO
                {
                    Id = a.Id,
                    Title = a.Title,
                    Body = a.Body,
                    Priority = a.Priority,
                    CreatedAt = a.CreatedAt,
                    PostedBy = a.PostedBy != null ? a.PostedBy.Name : "System",
                    IsRead = _db.AnnouncementReads.Any(r => r.AnnouncementId == a.Id && r.EmployeeId == userId)
                })
                .ToListAsync();

            return Ok(announcements);
        }

        [HttpPost]
        [Authorize(Roles = "HR")]
        public async Task<ActionResult<AnnouncementResponseDTO>> PostAnnouncement([FromBody] AnnouncementCreateDTO dto)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var announcement = new Announcement
            {
                Title = dto.Title,
                Body = dto.Body,
                Priority = dto.Priority,
                PostedByEmployeeId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Announcements.Add(announcement);
            await _db.SaveChangesAsync();

            // Reload to get PostedBy name
            var created = await _db.Announcements
                .Include(a => a.PostedBy)
                .FirstAsync(a => a.Id == announcement.Id);

            return CreatedAtAction(nameof(GetAnnouncements), new { id = announcement.Id }, new AnnouncementResponseDTO
            {
                Id = created.Id,
                Title = created.Title,
                Body = created.Body,
                Priority = created.Priority,
                CreatedAt = created.CreatedAt,
                PostedBy = created.PostedBy?.Name ?? "HR Admin",
                IsRead = false
            });
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var exists = await _db.Announcements.AnyAsync(a => a.Id == id);
            if (!exists) return NotFound();

            var alreadyRead = await _db.AnnouncementReads.AnyAsync(r => r.AnnouncementId == id && r.EmployeeId == userId);
            if (!alreadyRead)
            {
                _db.AnnouncementReads.Add(new AnnouncementRead
                {
                    AnnouncementId = id,
                    EmployeeId = userId,
                    ReadAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }

            return NoContent();
        }

        [HttpGet("unread-count")]
        public async Task<ActionResult<int>> GetUnreadCount()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId)) return Unauthorized();

            var totalCount = await _db.Announcements.CountAsync();
            var readCount = await _db.AnnouncementReads.CountAsync(r => r.EmployeeId == userId);

            // A more precise way if announcements can be deleted
            var unreadCount = await _db.Announcements
                .Where(a => !_db.AnnouncementReads.Any(r => r.AnnouncementId == a.Id && r.EmployeeId == userId))
                .CountAsync();

            return Ok(unreadCount);
        }
    }
}
