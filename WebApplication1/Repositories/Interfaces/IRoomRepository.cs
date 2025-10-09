using Fingerprint_Attendance_System.Models;

namespace Fingerprint_Attendance_System.Repositories.Interfaces;

public interface IRoomRepository
{
    Task<IEnumerable<Room>> GetAllAsync();
    Task<Room?> GetByIdAsync(int roomId);
    Task<bool> AddAsync(Room room);
    Task<bool> UpdateAsync(Room room);
    Task<bool> DeleteAsync(int roomId);
    Task<bool> ExistsAsync(int roomId);
}