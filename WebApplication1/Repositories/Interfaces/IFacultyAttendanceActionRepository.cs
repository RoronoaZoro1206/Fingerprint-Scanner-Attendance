using Fingerprint_Attendance_System.Models;

namespace Fingerprint_Attendance_System.Repositories.Interfaces
{
    public interface IFacultyAttendanceActionRepository
    {
        Task<bool> AddAsync(FacultyAttendanceAction action);
        Task<IEnumerable<FacultyAttendanceAction>> GetByFacultyIdAsync(int facId, int limit = 50);
        Task<IEnumerable<FacultyAttendanceAction>> GetByAttendanceIdAsync(int attendId);
        Task<IEnumerable<FacultyAttendanceAction>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<Dictionary<string, int>> GetActionStatsByFacultyAsync(int facId, DateTime startDate, DateTime endDate);
    }
}
