using EmployeeTracker.API.Data;
using EmployeeTracker.API.DTOs;
using EmployeeTracker.API.Models;
using EmployeeTracker.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace EmployeeTracker.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;

        public AuthController(AppDbContext db, IConfiguration config, IEmailService emailService)
        {
            _db = db;
            _config = config;
            _emailService = emailService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            var employee = await _db.Employees
                .FirstOrDefaultAsync(e => e.Email == dto.Email);

            if (employee == null)
                return Unauthorized(new { message = "Email not found. Please check and try again." });

            if (!employee.IsActive)
                return Unauthorized(new { message = "Account is deactivated. Please contact HR." });

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, employee.PasswordHash))
                return Unauthorized(new { message = "Incorrect password. Please try again." });

            // --- Device Binding Verification (Bypass for HR) ---
            if (employee.Role != "HR" && !string.IsNullOrEmpty(dto.DeviceId))
            {
                if (string.IsNullOrEmpty(employee.DeviceId))
                {
                    // 1. Employee has no device registered yet. 
                    // Check if *any* other employee already bound this device.
                    var existingDeviceUser = await _db.Employees
                        .FirstOrDefaultAsync(e => e.DeviceId == dto.DeviceId && e.Id != employee.Id);

                    if (existingDeviceUser != null)
                    {
                        return Unauthorized(new { message = "This device is already registered to another employee. Please use your own device." });
                    }

                    // 2. Bind the new device to this employee
                    employee.DeviceId = dto.DeviceId;
                    employee.DeviceName = dto.DeviceName;
                    await _db.SaveChangesAsync();
                }
                else
                {
                    // 3. Employee already has a device registered. It MUST match exactly.
                    if (employee.DeviceId != dto.DeviceId)
                    {
                        return Unauthorized(new { message = "Unauthorized Device. Please use your registered device or contact HR to reset pairing." });
                    }
                }
            }

            var token = GenerateJwtToken(employee.Id, employee.Email, employee.Role);

            return Ok(new AuthResponseDTO
            {
                Token = token,
                Role = employee.Role,
                EmployeeId = employee.Id,
                Name = employee.Name,
                Email = employee.Email,
                Mobile = employee.Mobile
            });
        }

        private string GenerateJwtToken(int id, string email, string role)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int employeeId))
            {
                return Unauthorized(new { message = "Invalid token." });
            }

            var employee = await _db.Employees.FindAsync(employeeId);
            if (employee == null) return NotFound(new { message = "Employee not found." });

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, employee.PasswordHash))
            {
                return BadRequest(new { message = "Incorrect current password." });
            }

            employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Password updated successfully." });
        }

        [HttpPost("forgot-password/send")]
        [AllowAnonymous]
        public async Task<IActionResult> SendForgotPasswordOtp([FromBody] ForgotPasswordSendDTO dto)
        {
            var employee = await _db.Employees
                .FirstOrDefaultAsync(e => (dto.Type == "email" ? e.Email == dto.Identifier : e.Mobile == dto.Identifier) && e.Role == dto.Role);

            if (employee == null)
            {
                return NotFound(new { message = "User not found with these details." });
            }

            // Generate 6-digit OTP
            var otpCode = new Random().Next(100000, 999999).ToString();

            // Save OTP to DB
            var otpRecord = new PasswordResetOtp
            {
                Email = employee.Email,
                Mobile = employee.Mobile,
                OtpCode = otpCode,
                Type = dto.Type,
                Role = dto.Role,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10)
            };

            _db.PasswordResetOtps.Add(otpRecord);
            await _db.SaveChangesAsync();

            // Send OTP
            if (dto.Type == "email")
            {
                try
                {
                    await _emailService.SendEmailAsync(employee.Email, employee.Name,
                        "Password Reset OTP",
                        $"<h3>Verification Code</h3><p>Your OTP for password reset is: <b>{otpCode}</b>. Valid for 10 minutes.</p>");
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = "Failed to send email.", error = ex.Message });
                }
            }
            else
            {
                // Mobile - Log for development
                Console.WriteLine($"[SMS DEBUG] Sender: 8891673297 (Employeetracker) | To: {dto.Identifier} | OTP: {otpCode}");
            }

            return Ok(new { message = $"OTP sent successfully to your {dto.Type}." });
        }

        [HttpPost("forgot-password/verify")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyForgotPasswordOtp([FromBody] ForgotPasswordVerifyDTO dto)
        {
            var otpRecord = await _db.PasswordResetOtps
                .Where(o => o.Role == dto.Role &&
                            (dto.Type == "email" ? o.Email == dto.Identifier : o.Mobile == dto.Identifier) &&
                            o.Type == dto.Type &&
                            o.OtpCode == dto.Otp &&
                            o.ExpiresAt > DateTime.UtcNow &&
                            !o.IsVerified)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (otpRecord == null)
            {
                return BadRequest(new { message = "Invalid or expired OTP." });
            }

            otpRecord.IsVerified = true;
            await _db.SaveChangesAsync();

            return Ok(new { message = "OTP verified successfully.", resetToken = "forgot-token" });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPasswordWithToken([FromBody] ResetPasswordWithTokenDTO dto)
        {
            if (dto.ResetToken == "forgot-token")
            {
                // Find the most recently verified OTP session
                var verifiedOtp = await _db.PasswordResetOtps
                    .Where(o => o.IsVerified && o.ExpiresAt > DateTime.UtcNow.AddMinutes(-20))
                    .OrderByDescending(o => o.CreatedAt)
                    .FirstOrDefaultAsync();

                if (verifiedOtp == null) return BadRequest(new { message = "Session expired. Please verify again." });

                var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Email == verifiedOtp.Email && e.Role == verifiedOtp.Role);
                if (employee == null) return NotFound(new { message = "Employee not found." });

                employee.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                await _db.SaveChangesAsync();

                return Ok(new { message = "Password reset successful." });
            }

            return BadRequest(new { message = "Invalid reset token." });
        }
    }
}