using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace EmployeeTracker.API.Hubs
{
    [Authorize]
    public class LocationHub : Hub
    {
        public async Task JoinEmployeeGroup(string employeeId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"employee_{employeeId}");
        }

        public async Task JoinHRGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "hr_group");
        }

        public async Task SendLocation(int employeeId, string employeeName, string jobRole,
                                        double latitude, double longitude)
        {
            var payload = new
            {
                EmployeeId = employeeId,
                EmployeeName = employeeName,
                JobRole = jobRole,
                Latitude = latitude,
                Longitude = longitude,
                Timestamp = DateTime.UtcNow
            };

            await Clients.Group("hr_group").SendAsync("ReceiveLocation", payload);
        }

        public async Task EmployeeLoggedOut(int employeeId)
        {
            await Clients.Group("hr_group").SendAsync("EmployeeOffline", employeeId);
        }
    }
}