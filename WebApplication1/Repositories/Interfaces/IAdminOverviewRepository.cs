using Fingerprint_Attendance_System.Models.DTOs;

namespace Fingerprint_Attendance_System.Repositories.Interfaces;

public interface IAdminOverviewRepository
{
    Task<AdminOverviewStatsDto> GetOverviewStatsAsync();
    Task<IEnumerable<RecentActivityDto>> GetRecentActivityAsync(int limit = 5);
}
