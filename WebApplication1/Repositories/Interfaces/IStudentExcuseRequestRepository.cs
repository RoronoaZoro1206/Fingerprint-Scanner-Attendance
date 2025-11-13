using Fingerprint_Attendance_System.Models;

namespace Fingerprint_Attendance_System.Repositories.Interfaces
{
    public interface IStudentExcuseRequestRepository
    {
        Task<StudentExcuseRequest?> GetByIdAsync(int excuseId);
        Task<IEnumerable<StudentExcuseRequest>> GetByStudentIdAsync(int studId);
        Task<IEnumerable<StudentExcuseRequest>> GetByClassIdAsync(int classId);
        Task<IEnumerable<StudentExcuseRequest>> GetPendingByFacultyAsync(int facId);
        Task<IEnumerable<StudentExcuseRequest>> GetByStatusAsync(string status);
        Task<bool> AddAsync(StudentExcuseRequest request);
        Task<bool> UpdateAsync(StudentExcuseRequest request);
        Task<bool> DeleteAsync(int excuseId);
        Task<int> GetPendingCountByFacultyAsync(int facId);
        Task<bool> ExistsForAttendanceDateAsync(int studId, int classId, DateTime date);
    }
}
