using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations
{
    public class FacultyRepository : IFacultyRepository
    {
        private readonly DatabaseHelper _db;

        public FacultyRepository(DatabaseHelper db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Faculty>> GetAllAsync()
        {
            string query = "SELECT * FROM faculty ORDER BY fac_id;";
            var dt = await _db.ExecuteQueryAsync(query);

            return dt.AsEnumerable().Select(row => new Faculty
            {
                FacId = Convert.ToInt32(row["fac_id"]),
                UserId = Convert.ToInt32(row["user_id"]),
                DeptId = row["dept_id"] != DBNull.Value ? Convert.ToInt32(row["dept_id"]) : null,
                FacRank = row["fac_rank"]?.ToString(),
                FacCreatedAt = Convert.ToDateTime(row["fac_created_at"])
            });
        }

        public async Task<Faculty?> GetByIdAsync(int facId)
        {
            string query = "SELECT * FROM faculty WHERE fac_id = @id;";
            var parameters = new Dictionary<string, object> { { "@id", facId } };

            var dt = await _db.ExecuteQueryAsync(query, parameters);
            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return new Faculty
            {
                FacId = Convert.ToInt32(row["fac_id"]),
                UserId = Convert.ToInt32(row["user_id"]),
                DeptId = row["dept_id"] != DBNull.Value ? Convert.ToInt32(row["dept_id"]) : null,
                FacRank = row["fac_rank"]?.ToString(),
                FacCreatedAt = Convert.ToDateTime(row["fac_created_at"])
            };
        }

        public async Task<Faculty?> GetByUserIdAsync(int userId)
        {
            string query = "SELECT * FROM faculty WHERE user_id = @user LIMIT 1;";
            var parameters = new Dictionary<string, object> { { "@user", userId } };

            var dt = await _db.ExecuteQueryAsync(query, parameters);
            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return new Faculty
            {
                FacId = Convert.ToInt32(row["fac_id"]),
                UserId = Convert.ToInt32(row["user_id"]),
                DeptId = row["dept_id"] != DBNull.Value ? Convert.ToInt32(row["dept_id"]) : null,
                FacRank = row["fac_rank"]?.ToString(),
                FacCreatedAt = Convert.ToDateTime(row["fac_created_at"])
            };
        }

        public async Task<IEnumerable<Faculty>> GetByDepartmentAsync(int deptId)
        {
            string query = "SELECT * FROM faculty WHERE dept_id = @dept ORDER BY fac_id;";
            var parameters = new Dictionary<string, object> { { "@dept", deptId } };

            var dt = await _db.ExecuteQueryAsync(query, parameters);
            return dt.AsEnumerable().Select(row => new Faculty
            {
                FacId = Convert.ToInt32(row["fac_id"]),
                UserId = Convert.ToInt32(row["user_id"]),
                DeptId = row["dept_id"] != DBNull.Value ? Convert.ToInt32(row["dept_id"]) : null,
                FacRank = row["fac_rank"]?.ToString(),
                FacCreatedAt = Convert.ToDateTime(row["fac_created_at"])
            });
        }

        public async Task<bool> AddAsync(Faculty faculty)
        {
            string query = @"
                INSERT INTO faculty (user_id, dept_id, fac_rank, fac_created_at)
                VALUES (@user, @dept, @rank, @created);";

            var parameters = new Dictionary<string, object>
            {
                { "@user", faculty.UserId },
                { "@dept", faculty.DeptId ?? (object)DBNull.Value },
                { "@rank", faculty.FacRank ?? (object)DBNull.Value },
                { "@created", faculty.FacCreatedAt }
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> UpdateAsync(Faculty faculty)
        {
            string query = @"
                UPDATE faculty
                SET dept_id = @dept,
                    fac_rank = @rank
                WHERE fac_id = @id;";

            var parameters = new Dictionary<string, object>
            {
                { "@id", faculty.FacId },
                { "@dept", faculty.DeptId ?? (object)DBNull.Value },
                { "@rank", faculty.FacRank ?? (object)DBNull.Value }
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> DeleteAsync(int facId)
        {
            string query = "DELETE FROM faculty WHERE fac_id = @id;";
            var parameters = new Dictionary<string, object> { { "@id", facId } };
            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> ExistsAsync(int facId)
        {
            string query = "SELECT COUNT(*) FROM faculty WHERE fac_id = @id;";
            var parameters = new Dictionary<string, object> { { "@id", facId } };
            var result = await _db.ExecuteScalarAsync(query, parameters);
            return Convert.ToInt32(result) > 0;
        }

        // Optimized DTO methods
        public async Task<IEnumerable<FacultyListDto>> GetAllWithDetailsAsync()
        {
            string query = @"
                SELECT 
                    f.fac_id,
                    f.user_id,
                    f.dept_id,
                    f.fac_rank,
                    f.fac_created_at,
                    CONCAT(u.user_first_name, ' ', u.user_last_name) AS fac_name,
                    u.user_email,
                    COALESCE(d.dept_name, 'No Department') AS dept_name
                FROM faculty f
                INNER JOIN users u ON f.user_id = u.user_id
                LEFT JOIN departments d ON f.dept_id = d.dept_id
                ORDER BY f.fac_id;";

            var dt = await _db.ExecuteQueryAsync(query);

            return dt.AsEnumerable().Select(row => new FacultyListDto
            {
                FacId = Convert.ToInt32(row["fac_id"]),
                UserId = Convert.ToInt32(row["user_id"]),
                FacName = row["fac_name"]?.ToString() ?? "Unknown",
                UserEmail = row["user_email"]?.ToString() ?? "",
                DeptId = row["dept_id"] != DBNull.Value ? Convert.ToInt32(row["dept_id"]) : null,
                DeptName = row["dept_name"]?.ToString() ?? "No Department",
                FacRank = row["fac_rank"]?.ToString() ?? "Not Specified",
                FacCreatedAt = Convert.ToDateTime(row["fac_created_at"])
            });
        }

        public async Task<FacultyDetailDto?> GetDetailByIdAsync(int facId)
        {
            string query = @"
                SELECT 
                    f.fac_id,
                    f.user_id,
                    f.dept_id,
                    f.fac_rank,
                    f.fac_created_at,
                    u.user_email,
                    u.user_first_name,
                    u.user_middle_name,
                    u.user_last_name,
                    u.user_phone,
                    d.dept_name
                FROM faculty f
                INNER JOIN users u ON f.user_id = u.user_id
                LEFT JOIN departments d ON f.dept_id = d.dept_id
                WHERE f.fac_id = @id
                LIMIT 1;";

            var parameters = new Dictionary<string, object> { { "@id", facId } };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return new FacultyDetailDto
            {
                FacId = Convert.ToInt32(row["fac_id"]),
                UserId = Convert.ToInt32(row["user_id"]),
                DeptId = row["dept_id"] != DBNull.Value ? Convert.ToInt32(row["dept_id"]) : null,
                FacRank = row["fac_rank"]?.ToString() ?? "",
                UserEmail = row["user_email"]?.ToString() ?? "",
                UserFirstName = row["user_first_name"]?.ToString() ?? "",
                UserMiddleName = row["user_middle_name"]?.ToString(),
                UserLastName = row["user_last_name"]?.ToString() ?? "",
                UserPhone = row["user_phone"]?.ToString(),
                DeptName = row["dept_name"]?.ToString(),
                FacCreatedAt = Convert.ToDateTime(row["fac_created_at"])
            };
        }

        public async Task<IEnumerable<FacultyUserDto>> GetAvailableFacultyUsersAsync()
        {
            string query = @"
                SELECT 
                    u.user_id,
                    CONCAT(u.user_first_name, ' ', u.user_last_name) AS user_name,
                    u.user_email,
                    CASE WHEN f.fac_id IS NOT NULL THEN 1 ELSE 0 END AS has_faculty_record
                FROM users u
                LEFT JOIN faculty f ON u.user_id = f.user_id
                WHERE u.role_id = 2
                ORDER BY u.user_first_name, u.user_last_name;";

            var dt = await _db.ExecuteQueryAsync(query);

            return dt.AsEnumerable().Select(row => new FacultyUserDto
            {
                UserId = Convert.ToInt32(row["user_id"]),
                UserName = row["user_name"]?.ToString() ?? "Unknown",
                UserEmail = row["user_email"]?.ToString() ?? "",
                HasFacultyRecord = Convert.ToInt32(row["has_faculty_record"]) == 1
            });
        }
    }
}
