using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations
{
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly DatabaseHelper _db;

        public DepartmentRepository(DatabaseHelper db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Department>> GetAllAsync()
        {
            string query = "SELECT * FROM departments ORDER BY dept_id;";
            var dt = await _db.ExecuteQueryAsync(query);

            return dt.AsEnumerable().Select(row => new Department
            {
                DeptId = Convert.ToInt32(row["dept_id"]),
                DeptCode = row["dept_code"].ToString() ?? "",
                DeptName = row["dept_name"].ToString() ?? "",
                DeptDesc = row["dept_desc"]?.ToString(),
                DeptCreatedAt = Convert.ToDateTime(row["dept_created_at"])
            });
        }

        public async Task<Department?> GetByIdAsync(int deptId)
        {
            string query = "SELECT * FROM departments WHERE dept_id = @id;";
            var parameters = new Dictionary<string, object> { { "@id", deptId } };

            var dt = await _db.ExecuteQueryAsync(query, parameters);
            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return new Department
            {
                DeptId = Convert.ToInt32(row["dept_id"]),
                DeptCode = row["dept_code"].ToString() ?? "",
                DeptName = row["dept_name"].ToString() ?? "",
                DeptDesc = row["dept_desc"]?.ToString(),
                DeptCreatedAt = Convert.ToDateTime(row["dept_created_at"])
            };
        }

        public async Task<Department?> GetByCodeAsync(string deptCode)
        {
            string query = "SELECT * FROM departments WHERE dept_code = @code;";
            var parameters = new Dictionary<string, object> { { "@code", deptCode } };

            var dt = await _db.ExecuteQueryAsync(query, parameters);
            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return new Department
            {
                DeptId = Convert.ToInt32(row["dept_id"]),
                DeptCode = row["dept_code"].ToString() ?? "",
                DeptName = row["dept_name"].ToString() ?? "",
                DeptDesc = row["dept_desc"]?.ToString(),
                DeptCreatedAt = Convert.ToDateTime(row["dept_created_at"])
            };
        }

        public async Task<bool> AddAsync(Department dept)
        {
            string query = @"
                INSERT INTO departments (dept_code, dept_name, dept_desc, dept_created_at)
                VALUES (@code, @name, @desc, @created);";

            var parameters = new Dictionary<string, object>
            {
                { "@code", dept.DeptCode },
                { "@name", dept.DeptName },
                { "@desc", dept.DeptDesc ?? (object)DBNull.Value },
                { "@created", dept.DeptCreatedAt }
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> UpdateAsync(Department dept)
        {
            string query = @"
                UPDATE departments SET
                    dept_code = @code,
                    dept_name = @name,
                    dept_desc = @desc
                WHERE dept_id = @id;";

            var parameters = new Dictionary<string, object>
            {
                { "@id", dept.DeptId },
                { "@code", dept.DeptCode },
                { "@name", dept.DeptName },
                { "@desc", dept.DeptDesc ?? (object)DBNull.Value }
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> DeleteAsync(int deptId)
        {
            string query = "DELETE FROM departments WHERE dept_id = @id;";
            var parameters = new Dictionary<string, object> { { "@id", deptId } };
            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> ExistsAsync(int deptId)
        {
            string query = "SELECT COUNT(*) FROM departments WHERE dept_id = @id;";
            var parameters = new Dictionary<string, object> { { "@id", deptId } };
            return await _db.RecordExistsAsync(query, parameters);
        }
    }
}
