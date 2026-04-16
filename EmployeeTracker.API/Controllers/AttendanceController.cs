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
    public class AttendanceController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IHubContext<LocationHub> _hub;

        public AttendanceController(AppDbContext db, IHubContext<LocationHub> hub)
        {
            _db = db;
            _hub = hub;
        }

        [HttpPost("checkin")]
        public async Task<IActionResult> CheckIn([FromBody] AttendanceCheckInDTO dto)
        {
            var employeeId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var today = DateTime.UtcNow.Date;

            var existing = await _db.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date == today);

            if (existing != null)
                return BadRequest(new { message = "Already checked in today." });

            var employee = await _db.Employees.FindAsync(employeeId);
            if (employee == null) return NotFound();

            var attendance = new Attendance
            {
                EmployeeId = employeeId,
                LoginTime = DateTime.UtcNow,
                LoginLatitude = dto.Latitude,
                LoginLongitude = dto.Longitude,
                Date = today,
                Status = "Present"
            };

            _db.Attendances.Add(attendance);
            _db.LocationLogs.Add(new LocationLog
            {
                EmployeeId = employeeId,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                IsActive = true
            });

            await _db.SaveChangesAsync();

            await _hub.Clients.Group("hr_group").SendAsync("EmployeeOnline", new
            {
                EmployeeId = employeeId,
                EmployeeName = employee.Name,
                JobRole = employee.JobRole,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Timestamp = DateTime.UtcNow
            });

            return Ok(MapToResponse(attendance, employee.Name));
        }

        [HttpPost("checkout")]
        public async Task<IActionResult> CheckOut([FromBody] AttendanceCheckOutDTO dto)
        {
            var employeeId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var today = DateTime.UtcNow.Date;

            var attendance = await _db.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date == today && a.LogoutTime == null);

            if (attendance == null)
                return BadRequest(new { message = "No active check-in found for today." });

            attendance.LogoutTime = DateTime.UtcNow;
            attendance.LogoutLatitude = dto.Latitude;
            attendance.LogoutLongitude = dto.Longitude;

            var activeLogs = await _db.LocationLogs
                .Where(l => l.EmployeeId == employeeId && l.IsActive)
                .ToListAsync();

            foreach (var log in activeLogs) log.IsActive = false;

            await _db.SaveChangesAsync();

            await _hub.Clients.Group("hr_group").SendAsync("EmployeeOffline", employeeId);

            var employee = await _db.Employees.FindAsync(employeeId);
            return Ok(MapToResponse(attendance, employee!.Name));
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyAttendance()
        {
            var employeeId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var records = await _db.Attendances
                .Include(a => a.Employee)
                .Where(a => a.EmployeeId == employeeId)
                .OrderByDescending(a => a.Date)
                .Take(30)
                .Select(a => MapToResponse(a, a.Employee.Name))
                .ToListAsync();
            return Ok(records);
        }

        [HttpGet("today")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> GetTodayAttendance()
        {
            var today = DateTime.UtcNow.Date;
            var records = await _db.Attendances
                .Include(a => a.Employee)
                .Where(a => a.Date == today)
                .OrderByDescending(a => a.LoginTime)
                .Select(a => MapToResponse(a, a.Employee.Name))
                .ToListAsync();
            return Ok(records);
        }

        [HttpGet("employee/{id}")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> GetByEmployee(int id)
        {
            var records = await _db.Attendances
                .Include(a => a.Employee)
                .Where(a => a.EmployeeId == id)
                .OrderByDescending(a => a.Date)
                .Select(a => MapToResponse(a, a.Employee.Name))
                .ToListAsync();
            return Ok(records);
        }

        [HttpGet("report")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> GetAttendanceReport([FromQuery] int? month, [FromQuery] int? year)
        {
            var targetMonth = month ?? DateTime.UtcNow.Month;
            var targetYear = year ?? DateTime.UtcNow.Year;
            
            var startDate = new DateTime(targetYear, targetMonth, 1, 0, 0, 0, DateTimeKind.Utc);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            
            // Get all employees
            var employees = await _db.Employees.Where(e => e.IsActive).ToListAsync();
            
            // Get all attendances for the month
            var attendances = await _db.Attendances
                .Where(a => a.Date >= startDate && a.Date <= endDate)
                .ToListAsync();
                
            // Get confirmed leaves for the month
            var leaves = await _db.LeaveRequests
                .Where(l => l.Status == "Approved" && l.FromDate <= endDate && l.ToDate >= startDate)
                .ToListAsync();

            var report = employees.Select(emp => {
                var presentDays = attendances.Count(a => a.EmployeeId == emp.Id);
                
                // Calculate leave days that fall strictly within this month for this employee
                var empLeaves = leaves.Where(l => l.EmployeeId == emp.Id).ToList();
                var leaveDays = 0;
                foreach (var l in empLeaves)
                {
                    var start = l.FromDate < startDate ? startDate : l.FromDate;
                    var end = l.ToDate > endDate ? endDate : l.ToDate;
                    leaveDays += (int)(end - start).TotalDays + 1;
                }
                
                var totalDaysInMonth = DateTime.DaysInMonth(targetYear, targetMonth);
                var absentDays = totalDaysInMonth - presentDays - leaveDays;
                if (absentDays < 0) absentDays = 0;

                return new {
                    EmployeeId = emp.Id,
                    EmployeeName = emp.Name,
                    JobRole = emp.JobRole,
                    TotalPresent = presentDays,
                    TotalLeave = leaveDays,
                    TotalAbsent = absentDays
                };
            }).ToList();

            return Ok(new {
                Month = targetMonth,
                Year = targetYear,
                TotalDays = DateTime.DaysInMonth(targetYear, targetMonth),
                Data = report
            });
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetTodayStatus()
        {
            var employeeId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var today = DateTime.UtcNow.Date;

            var attendance = await _db.Attendances
                .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date == today);

            if (attendance == null)
                return Ok(new { isCheckedIn = false, isCheckedOut = false, attendance = (object?)null });

            return Ok(new
            {
                isCheckedIn = true,
                isCheckedOut = attendance.LogoutTime.HasValue,
                attendance = MapToResponse(attendance, "")
            });
        }

        private static AttendanceResponseDTO MapToResponse(Attendance a, string name) => new()
        {
            Id = a.Id,
            EmployeeId = a.EmployeeId,
            EmployeeName = name,
            LoginTime = DateTime.SpecifyKind(a.LoginTime, DateTimeKind.Utc),
            LogoutTime = a.LogoutTime.HasValue
                ? DateTime.SpecifyKind(a.LogoutTime.Value, DateTimeKind.Utc)
                : null,
            LoginLatitude = a.LoginLatitude,
            LoginLongitude = a.LoginLongitude,
            LogoutLatitude = a.LogoutLatitude,
            LogoutLongitude = a.LogoutLongitude,
            Date = a.Date,
            Status = a.Status
        };
    }
}