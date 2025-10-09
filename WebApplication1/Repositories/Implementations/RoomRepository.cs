using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations;
public class RoomRepository : IRoomRepository
{
    private readonly DatabaseHelper _db;

    public RoomRepository(DatabaseHelper db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Room>> GetAllAsync()
    {
        string query = "SELECT room_id, room_code, room_description FROM rooms;";
        var dt = await _db.ExecuteQueryAsync(query);

        return dt.AsEnumerable().Select(row => new Room
        {
            RoomId = Convert.ToInt32(row["room_id"]),
            RoomCode = row["room_code"].ToString() ?? "",
            RoomDescription = row["room_description"]?.ToString()
        });
    }

    public async Task<Room?> GetByIdAsync(int roomId)
    {
        string query = "SELECT room_id, room_code, room_description FROM rooms WHERE room_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", roomId } };

        var dt = await _db.ExecuteQueryAsync(query, parameters);
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new Room
        {
            RoomId = Convert.ToInt32(row["room_id"]),
            RoomCode = row["room_code"].ToString() ?? "",
            RoomDescription = row["room_description"]?.ToString()
        };
    }

    public async Task<bool> AddAsync(Room room)
    {
        string query = "INSERT INTO rooms (room_code, room_description) VALUES (@code, @desc);";
        var parameters = new Dictionary<string, object>
        {
            { "@code", room.RoomCode },
            { "@desc", room.RoomDescription ?? (object)DBNull.Value }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> UpdateAsync(Room room)
    {
        string query = "UPDATE rooms SET room_code = @code, room_description = @desc WHERE room_id = @id;";
        var parameters = new Dictionary<string, object>
        {
            { "@id", room.RoomId },
            { "@code", room.RoomCode },
            { "@desc", room.RoomDescription ?? (object)DBNull.Value }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> DeleteAsync(int roomId)
    {
        string query = "DELETE FROM rooms WHERE room_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", roomId } };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> ExistsAsync(int roomId)
    {
        string query = "SELECT COUNT(*) FROM rooms WHERE room_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", roomId } };
        return await _db.RecordExistsAsync(query, parameters);
    }
}
