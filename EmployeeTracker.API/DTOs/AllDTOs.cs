namespace EmployeeTracker.API.DTOs
{
    public class LoginDTO
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? DeviceId { get; set; }
        public string? DeviceName { get; set; }
    }

    public class AuthResponseDTO
    {
        public string Token { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int EmployeeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
    }

    public class EmployeeCreateDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string JobRole { get; set; } = string.Empty;
        public string? ProfilePhoto { get; set; }
    }

    public class EmployeeResponseDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string JobRole { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ProfilePhoto { get; set; }
    }

    public class EmployeeUpdateDTO
    {
        public string? Name { get; set; }
        public string? Mobile { get; set; }
        public string? JobRole { get; set; }
    }

    public class AttendanceCheckInDTO
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class AttendanceCheckOutDTO
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class AttendanceResponseDTO
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public DateTime LoginTime { get; set; }
        public DateTime? LogoutTime { get; set; }
        public double LoginLatitude { get; set; }
        public double LoginLongitude { get; set; }
        public double? LogoutLatitude { get; set; }
        public double? LogoutLongitude { get; set; }
        public DateTime Date { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class LocationUpdateDTO
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class LiveLocationDTO
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string JobRole { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class LeaveRequestCreateDTO
    {
        public string LeaveType { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string Reason { get; set; } = string.Empty;
    }

    public class LeaveActionDTO
    {
        public string Status { get; set; } = string.Empty;
        public string? HRComment { get; set; }
    }

    public class LeaveResponseDTO
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string LeaveType { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? HRComment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ResetPasswordDTO
    {
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ChangePasswordDTO
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class LocationSilentDTO
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
    }

    public class ForgotPasswordSendDTO
    {
        public string Role { get; set; } = string.Empty; // "HR" or "Employee"
        public string Identifier { get; set; } = string.Empty; // Email or Mobile
        public string Type { get; set; } = string.Empty; // "email" or "mobile"
    }

    public class ForgotPasswordVerifyDTO
    {
        public string Role { get; set; } = string.Empty;
        public string Identifier { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Otp { get; set; } = string.Empty;
    }

    public class ResetPasswordWithTokenDTO
    {
        public string ResetToken { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class AnnouncementCreateDTO
    {
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Priority { get; set; } = "Low";
    }

    public class AnnouncementResponseDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string PostedBy { get; set; } = string.Empty;
        public bool IsRead { get; set; }
    }
}