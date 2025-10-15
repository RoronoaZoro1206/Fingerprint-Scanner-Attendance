using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations;
public class AttendanceRepository : IAttendanceRepository
{
    private readonly DatabaseHelper _db;

    public AttendanceRepository(DatabaseHelper db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Attendance>> GetAllAsync()
    {
        string query = @"SELECT attend_id, class_id, stud_id, attend_date, attend_scan_time, attend_type, attend_status, 
                         attend_remark, attend_created_at FROM attendance;";
        var dt = await _db.ExecuteQueryAsync(query);

        return dt.AsEnumerable().Select(row => new Attendance
        {
            AttendId = Convert.ToInt32(row["attend_id"]),
            ClassId = row["class_id"] != DBNull.Value ? Convert.ToInt32(row["class_id"]) : (int?)null,
            StudId = row["stud_id"] != DBNull.Value ? Convert.ToInt32(row["stud_id"]) : (int?)null,
            AttendDate = Convert.ToDateTime(row["attend_date"]),
            AttendScanTime = Convert.ToDateTime(row["attend_scan_time"]),
            AttendType = Enum.Parse<ScanType>(row["attend_type"].ToString() ?? "In", true),
            AttendStatus = Enum.Parse<AttendanceStatus>(row["attend_status"].ToString() ?? "Pending", true),
            AttendRemark = row["attend_remark"]?.ToString(),
            AttendCreatedAt = Convert.ToDateTime(row["attend_created_at"])
        });
    }

    public async Task<Attendance?> GetByIdAsync(int attendId)
    {
        string query = @"SELECT attend_id, class_id, stud_id, attend_date, attend_scan_time, attend_type, attend_status, 
                         attend_remark, attend_created_at FROM attendance WHERE attend_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", attendId } };

        var dt = await _db.ExecuteQueryAsync(query, parameters);
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new Attendance
        {
            AttendId = Convert.ToInt32(row["attend_id"]),
            ClassId = row["class_id"] != DBNull.Value ? Convert.ToInt32(row["class_id"]) : (int?)null,
            StudId = row["stud_id"] != DBNull.Value ? Convert.ToInt32(row["stud_id"]) : (int?)null,
            AttendDate = Convert.ToDateTime(row["attend_date"]),
            AttendScanTime = Convert.ToDateTime(row["attend_scan_time"]),
            AttendType = Enum.Parse<ScanType>(row["attend_type"].ToString() ?? "In", true),
            AttendStatus = Enum.Parse<AttendanceStatus>(row["attend_status"].ToString() ?? "Pending", true),
            AttendRemark = row["attend_remark"]?.ToString(),
            AttendCreatedAt = Convert.ToDateTime(row["attend_created_at"])
        };
    }

    public async Task<bool> AddAsync(Attendance attendance)
    {
        string query = @"INSERT INTO attendance (class_id, stud_id, attend_date, attend_scan_time, attend_type, attend_status, attend_remark, attend_created_at)
                         VALUES (@class, @stud, @date, @scantime, @type::scan_type_enum, @status::attendance_status, @remark, @created);";
        var parameters = new Dictionary<string, object>
        {
            { "@class", attendance.ClassId ?? (object)DBNull.Value },
            { "@stud", attendance.StudId ?? (object)DBNull.Value },
            { "@date", attendance.AttendDate },
            { "@scantime", attendance.AttendScanTime },
            { "@type", attendance.AttendType.ToString().ToLower() },
            { "@status", attendance.AttendStatus.ToString().ToLower() },
            { "@remark", attendance.AttendRemark ?? (object)DBNull.Value },
            { "@created", attendance.AttendCreatedAt }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> UpdateAsync(Attendance attendance)
    {
        string query = @"UPDATE attendance 
                         SET class_id = @class, stud_id = @stud, attend_date = @date, attend_scan_time = @scantime,
                             attend_type = @type::scan_type_enum, attend_status = @status::attendance_status, attend_remark = @remark,
                             attend_created_at = @created
                         WHERE attend_id = @id;";
        var parameters = new Dictionary<string, object>
        {
            { "@id", attendance.AttendId },
            { "@class", attendance.ClassId ?? (object)DBNull.Value },
            { "@stud", attendance.StudId ?? (object)DBNull.Value },
            { "@date", attendance.AttendDate },
            { "@scantime", attendance.AttendScanTime },
            { "@type", attendance.AttendType.ToString().ToLower() },
            { "@status", attendance.AttendStatus.ToString().ToLower() },
            { "@remark", attendance.AttendRemark ?? (object)DBNull.Value },
            { "@created", attendance.AttendCreatedAt }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> DeleteAsync(int attendId)
    {
        string query = "DELETE FROM attendance WHERE attend_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", attendId } };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> ExistsAsync(int attendId)
    {
        string query = "SELECT COUNT(*) FROM attendance WHERE attend_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", attendId } };
        return await _db.RecordExistsAsync(query, parameters);
    }
}
