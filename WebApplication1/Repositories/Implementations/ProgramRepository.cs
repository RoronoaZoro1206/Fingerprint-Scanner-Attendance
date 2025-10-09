using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations;
public class ProgramRepository : IProgramRepository
{
    private readonly DatabaseHelper _db;

    public ProgramRepository(DatabaseHelper db)
    {
        _db = db;
    }

    public async Task<IEnumerable<ProgramModel>> GetAllAsync()
    {
        string query = "SELECT * FROM programs ORDER BY program_id;";
        var dt = await _db.ExecuteQueryAsync(query);

        return dt.AsEnumerable().Select(row => new ProgramModel
        {
            ProgramId = Convert.ToInt32(row["program_id"]),
            ProgramCode = row["program_code"].ToString() ?? "",
            ProgramName = row["program_name"].ToString() ?? "",
            ProgramDesc = row["program_desc"]?.ToString(),
            ProgramCreatedAt = Convert.ToDateTime(row["program_created_at"])
        });
    }

    public async Task<ProgramModel?> GetByIdAsync(int programId)
    {
        string query = "SELECT * FROM programs WHERE program_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", programId } };

        var dt = await _db.ExecuteQueryAsync(query, parameters);
        if (dt.Rows.Count == 0) return null;

        var row = dt.Rows[0];
        return new ProgramModel
        {
            ProgramId = Convert.ToInt32(row["program_id"]),
            ProgramCode = row["program_code"].ToString() ?? "",
            ProgramName = row["program_name"].ToString() ?? "",
            ProgramDesc = row["program_desc"]?.ToString(),
            ProgramCreatedAt = Convert.ToDateTime(row["program_created_at"])
        };
    }

    public async Task<bool> AddAsync(ProgramModel program)
    {
        string query = @"
            INSERT INTO programs (program_code, program_name, program_desc, program_created_at)
            VALUES (@code, @name, @desc, @created);";

        var parameters = new Dictionary<string, object>
        {
            { "@code", program.ProgramCode },
            { "@name", program.ProgramName },
            { "@desc", program.ProgramDesc ?? (object)DBNull.Value },
            { "@created", program.ProgramCreatedAt }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> UpdateAsync(ProgramModel program)
    {
        string query = @"
            UPDATE programs
            SET program_code = @code,
                program_name = @name,
                program_desc = @desc
            WHERE program_id = @id;";

        var parameters = new Dictionary<string, object>
        {
            { "@id", program.ProgramId },
            { "@code", program.ProgramCode },
            { "@name", program.ProgramName },
            { "@desc", program.ProgramDesc ?? (object)DBNull.Value }
        };

        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }

    public async Task<bool> DeleteAsync(int programId)
    {
        string query = "DELETE FROM programs WHERE program_id = @id;";
        var parameters = new Dictionary<string, object> { { "@id", programId } };
        return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
    }
}