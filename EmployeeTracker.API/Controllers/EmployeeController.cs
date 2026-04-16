using EmployeeTracker.API.Data;
using EmployeeTracker.API.DTOs;
using EmployeeTracker.API.Models;
using EmployeeTracker.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static EmployeeTracker.API.DTOs.LeaveResponseDTO;

namespace EmployeeTracker.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EmployeeController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IWebHostEnvironment _env;
        private readonly IEmailService _email;

        public EmployeeController(AppDbContext db, IWebHostEnvironment env, IEmailService email)
        {
            _db = db;
            _env = env;
            _email = email;
        }

        [HttpPost]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> RegisterEmployee([FromBody] EmployeeCreateDTO dto)
        {
            if (await _db.Employees.AnyAsync(e => e.Email == dto.Email))
                return BadRequest(new { message = "Email already exists." });

            // Validate password strength
            var passwordError = ValidatePassword(dto.Password);
            if (passwordError != null)
                return BadRequest(new { message = passwordError });

            var employee = new Employee
            {
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Mobile = dto.Mobile,
                JobRole = dto.JobRole,
                Role = "Employee"
            };

            _db.Employees.Add(employee);
            await _db.SaveChangesAsync();

            // Send welcome email with credentials
            try
            {
                await _email.SendPasswordEmailAsync(employee.Email, employee.Name, dto.Password);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email sending failed: {ex.Message}");
            }

            return Ok(MapToResponse(employee));
        }

        [HttpPost("{id}/photo")]
        public async Task<IActionResult> UploadPhoto(int id, IFormFile photo)
        {
            var callerId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var callerRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)!.Value;

            if (callerRole != "HR" && callerId != id) return Forbid();

            var employee = await _db.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            if (photo == null || photo.Length == 0)
                return BadRequest(new { message = "No photo provided." });

            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png" };
            if (!allowedTypes.Contains(photo.ContentType.ToLower()))
                return BadRequest(new { message = "Only JPG and PNG allowed." });

            var uploadsFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "profiles");
            Directory.CreateDirectory(uploadsFolder);

            if (!string.IsNullOrEmpty(employee.ProfilePhoto))
            {
                var oldPath = Path.Combine(_env.WebRootPath ?? "wwwroot", employee.ProfilePhoto.TrimStart('/'));
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }

            var fileName = $"{id}_{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
                await photo.CopyToAsync(stream);

            employee.ProfilePhoto = $"/uploads/profiles/{fileName}";
            await _db.SaveChangesAsync();

            return Ok(new { message = "Photo uploaded.", profilePhoto = employee.ProfilePhoto });
        }

        [HttpGet]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> GetAll()
        {
            var employees = await _db.Employees
                .Where(e => e.Role == "Employee")
                .OrderBy(e => e.Name)
                .Select(e => MapToResponse(e))
                .ToListAsync();
            return Ok(employees);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var callerId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var callerRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)!.Value;

            if (callerRole != "HR" && callerId != id) return Forbid();

            var employee = await _db.Employees.FindAsync(id);
            if (employee == null) return NotFound();
            return Ok(MapToResponse(employee));
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] EmployeeUpdateDTO dto)
        {
            var callerId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var callerRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)!.Value;

            if (callerRole != "HR" && callerId != id) return Forbid();

            var employee = await _db.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            if (dto.Name != null) employee.Name = dto.Name;
            if (dto.Mobile != null) employee.Mobile = dto.Mobile;
            if (dto.JobRole != null) employee.JobRole = dto.JobRole;

            await _db.SaveChangesAsync();
            return Ok(MapToResponse(employee));
        }

        // Toggle employee active/inactive (deactivate or reactivate)
        [HttpPut("{id}/deactivate")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> Deactivate(int id)
        {
            var employee = await _db.Employees.FindAsync(id);
            if (employee == null) return NotFound();
            employee.IsActive = !employee.IsActive;
            await _db.SaveChangesAsync();
            var action = employee.IsActive ? "activated" : "deactivated";
            return Ok(new { message = $"Employee {action}.", isActive = employee.IsActive });
        }

        // Permanently delete employee
        [HttpDelete("{id}")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> PermanentDelete(int id)
        {
            var employee = await _db.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            // Delete profile photo if exists
            if (!string.IsNullOrEmpty(employee.ProfilePhoto))
            {
                var oldPath = Path.Combine(_env.WebRootPath ?? "wwwroot", employee.ProfilePhoto.TrimStart('/'));
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
            }

            _db.Employees.Remove(employee);
            await _db.SaveChangesAsync();
            return Ok(new { message = "Employee permanently deleted." });
        }

        // Reset password by HR
        [HttpPut("{id}/reset-password")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordDTO dto)
        {
            var employee = await _db.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            var passwordError = ValidatePassword(dto.NewPassword);
            if (passwordError != null)
                return BadRequest(new { message = passwordError });

            employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _db.SaveChangesAsync();

            // Send reset email
            try
            {
                await _email.SendPasswordResetEmailAsync(employee.Email, employee.Name, dto.NewPassword);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email sending failed: {ex.Message}");
            }

            return Ok(new { message = "Password reset and emailed to employee." });
        }

        // Reset Device Binding by HR
        [HttpPost("{id}/reset-device")]
        [Authorize(Roles = "HR")]
        public async Task<IActionResult> ResetDevice(int id)
        {
            var employee = await _db.Employees.FindAsync(id);
            if (employee == null) return NotFound(new { message = "Employee not found." });

            employee.DeviceId = null;
            employee.DeviceName = null;
            await _db.SaveChangesAsync();

            return Ok(new { message = "Device pairing has been successfully reset. The employee can now log in from a new device." });
        }

        private static string? ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return "Password must be at least 8 characters.";
            if (!password.Any(char.IsUpper))
                return "Password must contain at least one uppercase letter.";
            if (!password.Any(char.IsLower))
                return "Password must contain at least one lowercase letter.";
            if (!password.Any(char.IsDigit))
                return "Password must contain at least one number.";
            if (!password.Any(c => "!@#$%^&*()_+-=[]{}|;':\",./<>?".Contains(c)))
                return "Password must contain at least one special character (!@#$%^&*).";
            return null;
        }

        private static EmployeeResponseDTO MapToResponse(Employee e) => new()
        {
            Id = e.Id,
            Name = e.Name,
            Email = e.Email,
            Mobile = e.Mobile,
            JobRole = e.JobRole,
            Role = e.Role,
            IsActive = e.IsActive,
            CreatedAt = e.CreatedAt,
            ProfilePhoto = e.ProfilePhoto
        };
    }
}