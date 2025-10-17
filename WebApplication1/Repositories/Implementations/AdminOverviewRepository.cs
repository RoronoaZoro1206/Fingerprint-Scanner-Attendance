using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models.DTOs;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations;

public class AdminOverviewRepository : IAdminOverviewRepository
{
    private readonly DatabaseHelper _db;

    public AdminOverviewRepository(DatabaseHelper db)
    {
        _db = db;
    }

    /// <summary>
    /// Gets overview statistics with a single optimized query
    /// </summary>
    public async Task<AdminOverviewStatsDto> GetOverviewStatsAsync()
    {
        string query = @"
            SELECT 
                (SELECT COUNT(*) FROM users) as total_users,
                (SELECT COUNT(*) FROM courses) as total_courses,
                (SELECT COUNT(*) FROM faculty) as total_faculty,
                (SELECT COUNT(*) FROM rooms) as total_rooms;";

        var dt = await _db.ExecuteQueryAsync(query);
        
        if (dt.Rows.Count == 0)
        {
            return new AdminOverviewStatsDto();
        }

        var row = dt.Rows[0];
        return new AdminOverviewStatsDto
        {
            TotalUsers = Convert.ToInt32(row["total_users"]),
            TotalCourses = Convert.ToInt32(row["total_courses"]),
            TotalFaculty = Convert.ToInt32(row["total_faculty"]),
            TotalRooms = Convert.ToInt32(row["total_rooms"])
        };
    }

    /// <summary>
    /// Gets recent activity with a single optimized query
    /// </summary>
    public async Task<IEnumerable<RecentActivityDto>> GetRecentActivityAsync(int limit = 5)
    {
        string query = @"
            SELECT 
                aud_action,
                aud_details,
                aud_action_time
            FROM administration_audit
            ORDER BY aud_action_time DESC
            LIMIT @limit;";

        var parameters = new Dictionary<string, object>
        {
            { "@limit", limit }
        };

        var dt = await _db.ExecuteQueryAsync(query, parameters);

        return dt.AsEnumerable().Select(row => new RecentActivityDto
        {
            Action = row["aud_action"]?.ToString() ?? string.Empty,
            Details = row["aud_details"]?.ToString() ?? string.Empty,
            Timestamp = Convert.ToDateTime(row["aud_action_time"])
        });
    }
}
