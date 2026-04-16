using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using EmployeeTracker.API.Data;
using EmployeeTracker.API.DTOs;
using EmployeeTracker.API.Hubs;
using EmployeeTracker.API.Models;

namespace EmployeeTracker.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LocationController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IHubContext<LocationHub> _hub;

        public LocationController(AppDbContext db, IHubContext<LocationHub> hub)
        {
            _db = db;
            _hub = hub;
        }

        [HttpPost("update")]
        public async Task<IActionResult> UpdateLocation([FromBody] LocationUpdateDTO dto)
        {
            var employeeId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var today = DateTime.UtcNow.Date;

            var checkedIn = await _db.Attendances
                .AnyAsync(a => a.EmployeeId == employeeId && a.Date == today && a.LogoutTime == null);

            if (!checkedIn)
                return BadRequest(new { message = "You must be checked in to share location." });

            var employee = await _db.Employees.FindAsync(employeeId);
            if (employee == null) return NotFound();

            _db.LocationLogs.Add(new LocationLog
            {
                EmployeeId = employeeId,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                IsActive = true,
                Timestamp = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            return Ok(new { message = "Location updated." });
        }

        [HttpGet("live")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> GetLiveLocations()
        {
            var today = DateTime.UtcNow.Date;

            // 1. Get IDs of employees who are currently checked in
            var checkedInIds = await _db.Attendances
                .Where(a => a.Date == today && a.LogoutTime == null)
                .Select(a => a.EmployeeId)
                .ToListAsync();

            if (!checkedInIds.Any()) return Ok(new List<LiveLocationDTO>());

            // 2. Optimized Query: GroupBy is incredibly slow on large tables in EF Core.
            // Using N index-seeks is significantly faster for live location tracking.
            var latestLocations = new List<LocationLog>();
            foreach (var id in checkedInIds)
            {
                var latest = await _db.LocationLogs
                    .Include(l => l.Employee)
                    .Where(l => l.EmployeeId == id && l.IsActive)
                    .OrderByDescending(l => l.Timestamp)
                    .FirstOrDefaultAsync();

                if (latest != null) latestLocations.Add(latest);
            }

            var liveLocations = latestLocations
                .Where(l => l != null && l.Employee != null)
                .Select(l => new LiveLocationDTO
                {
                    EmployeeId = l!.EmployeeId,
                    EmployeeName = l.Employee!.Name,
                    JobRole = l.Employee.JobRole,
                    Latitude = l.Latitude,
                    Longitude = l.Longitude,
                    Timestamp = l.Timestamp
                })
                .ToList();

            return Ok(liveLocations);
        }

        [HttpGet("history/{employeeId}")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> GetLocationHistory(int employeeId)
        {
            var today = DateTime.UtcNow.Date;
            var logs = await _db.LocationLogs
                .Where(l => l.EmployeeId == employeeId && l.Timestamp.Date == today)
                .OrderByDescending(l => l.Timestamp)
                .Select(l => new { l.Latitude, l.Longitude, l.Timestamp })
                .ToListAsync();
            return Ok(logs);
        }

        // Called by employee app when GPS has been silent for 4+ minutes
        [HttpPost("silent")]
        public async Task<IActionResult> ReportSilent([FromBody] LocationSilentDTO dto)
        {
            var lastLog = await _db.LocationLogs
                .Where(l => l.EmployeeId == dto.EmployeeId)
                .OrderByDescending(l => l.Timestamp)
                .FirstOrDefaultAsync();

            // Notify all HR clients via SignalR
            await _hub.Clients.Group("hr_group").SendAsync("LocationSilent", new
            {
                dto.EmployeeId,
                dto.EmployeeName,
                LastLatitude  = lastLog?.Latitude,
                LastLongitude = lastLog?.Longitude,
                LastSeen      = lastLog?.Timestamp,
                AlertTime     = DateTime.UtcNow
            });

            return Ok(new { message = "HR notified" });
        }
    }
}
