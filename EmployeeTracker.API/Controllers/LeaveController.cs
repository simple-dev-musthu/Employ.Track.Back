using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using EmployeeTracker.API.Data;
using EmployeeTracker.API.DTOs;
using EmployeeTracker.API.Models;
using EmployeeTracker.API.Services;

namespace EmployeeTracker.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LeaveController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IEmailService _email;

        public LeaveController(AppDbContext db, IEmailService email)
        {
            _db = db;
            _email = email;
        }

        [HttpPost]
        public async Task<IActionResult> Apply([FromBody] LeaveRequestCreateDTO dto)
        {
            var employeeId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

            if (dto.FromDate > dto.ToDate)
                return BadRequest(new { message = "From date cannot be after To date." });

            var leave = new LeaveRequest
            {
                EmployeeId = employeeId,
                LeaveType = dto.LeaveType,
                FromDate = dto.FromDate,
                ToDate = dto.ToDate,
                Reason = dto.Reason,
                Status = "Pending"
            };

            _db.LeaveRequests.Add(leave);
            await _db.SaveChangesAsync();
            return Ok(await MapAsync(leave));
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyLeaves()
        {
            var employeeId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            var leaves = await _db.LeaveRequests
                .Include(l => l.Employee)
                .Where(l => l.EmployeeId == employeeId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
            return Ok(leaves.Select(l => Map(l, l.Employee.Name)));
        }

        [HttpGet("all")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> GetAll([FromQuery] string? status = null)
        {
            var query = _db.LeaveRequests.Include(l => l.Employee).AsQueryable();
            if (!string.IsNullOrEmpty(status))
                query = query.Where(l => l.Status == status);
            var leaves = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();
            return Ok(leaves.Select(l => Map(l, l.Employee.Name)));
        }

        [HttpPut("{id}/action")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> Action(int id, [FromBody] LeaveActionDTO dto)
        {
            if (dto.Status != "Approved" && dto.Status != "Rejected")
                return BadRequest(new { message = "Status must be Approved or Rejected." });

            var leave = await _db.LeaveRequests
                .Include(l => l.Employee)
                .FirstOrDefaultAsync(l => l.Id == id);
            if (leave == null) return NotFound();

            var hrId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            leave.Status = dto.Status;
            leave.HRComment = dto.HRComment;
            leave.ApprovedById = hrId;
            leave.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            // --- Send Email Notification to Employee ---
            if (leave.Employee != null && !string.IsNullOrEmpty(leave.Employee.Email))
            {
                var isApproved = dto.Status == "Approved";
                var color = isApproved ? "#10B981" : "#EF4444"; // Green or Red
                var emoji = isApproved ? "✅" : "❌";
                
                var subject = $"Leave Request {dto.Status} - EmployeeTracker";
                var body = $@"
<!DOCTYPE html>
<html>
<body style='font-family: Arial, sans-serif; background-color: #F8FAFC; padding: 20px;'>
  <div style='max-width: 500px; margin: 0 auto; background: #fff; border-radius: 12px; padding: 30px; border-top: 5px solid {color}; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1);'>
    <h2 style='margin-top:0; color: #0F172A;'>{emoji} Leave Request {dto.Status}</h2>
    <p style='color: #475569; font-size: 16px;'>Hi {leave.Employee.Name},</p>
    <p style='color: #475569; font-size: 16px; line-height: 1.5;'>HR has review your leave request for <strong>{leave.LeaveType}</strong> from <strong>{leave.FromDate:MMM dd, yyyy}</strong> to <strong>{leave.ToDate:MMM dd, yyyy}</strong>.</p>
    
    <div style='background: #F1F5F9; border-radius: 8px; padding: 16px; margin: 20px 0;'>
      <p style='margin:0; color: #64748B; font-size: 14px; text-transform: uppercase; font-weight: bold;'>Status</p>
      <p style='margin: 4px 0 0; color: {color}; font-size: 18px; font-weight: bold;'>{dto.Status}</p>
    </div>

    {(string.IsNullOrEmpty(dto.HRComment) ? "" : $@"
    <div style='background: #F8FAFC; border-left: 4px solid #CBD5E1; padding: 12px 16px; margin: 20px 0;'>
      <p style='margin:0; color: #334155; font-style: italic;'>""{dto.HRComment}""</p>
    </div>
    ")}
    
    <p style='color: #94A3B8; font-size: 14px; margin-top: 30px; border-top: 1px solid #E2E8F0; padding-top: 20px;'>Log in to EmployeeTracker to view your attendance dashboard.</p>
  </div>
</body>
</html>";
                try {
                    await ((EmailService)_email).SendEmailAsync(leave.Employee.Email, leave.Employee.Name, subject, body);
                } catch (Exception ex) {
                    Console.WriteLine($"[Email Error] {ex.Message}");
                }
            }

            return Ok(Map(leave, leave.Employee?.Name ?? "Unknown"));
        }

        [HttpGet("pending-count")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> PendingCount()
        {
            var count = await _db.LeaveRequests.CountAsync(l => l.Status == "Pending");
            return Ok(new { count });
        }

        private async Task<LeaveResponseDTO> MapAsync(LeaveRequest l)
        {
            var emp = await _db.Employees.FindAsync(l.EmployeeId);
            return Map(l, emp?.Name ?? "");
        }

        private static LeaveResponseDTO Map(LeaveRequest l, string name) => new()
        {
            Id = l.Id,
            EmployeeId = l.EmployeeId,
            EmployeeName = name,
            LeaveType = l.LeaveType,
            FromDate = l.FromDate,
            ToDate = l.ToDate,
            Reason = l.Reason,
            Status = l.Status,
            HRComment = l.HRComment,
            CreatedAt = l.CreatedAt
        };
    }
}