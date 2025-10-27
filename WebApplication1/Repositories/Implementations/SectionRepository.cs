using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Models.DTOs;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations
{
    public class SectionRepository : ISectionRepository
    {
        private readonly DatabaseHelper _db;

        public SectionRepository(DatabaseHelper db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Section>> GetAllAsync()
        {
            string query = "SELECT * FROM sections ORDER BY section_id;";
            var dt = await _db.ExecuteQueryAsync(query);

            return dt.AsEnumerable().Select(row => new Section
            {
                SectionId = Convert.ToInt32(row["section_id"]),
                SectionName = row["section_name"].ToString() ?? "",
                ProgramId = row["program_id"] != DBNull.Value ? Convert.ToInt32(row["program_id"]) : 0,
                YearLevel = row["year_level"] != DBNull.Value ? Convert.ToInt32(row["year_level"]) : 0,
                SectionCreatedAt = Convert.ToDateTime(row["section_created_at"])
            });
        }

        public async Task<Section?> GetByIdAsync(int sectionId)
        {
            string query = "SELECT * FROM sections WHERE section_id = @id;";
            var parameters = new Dictionary<string, object> { { "@id", sectionId } };

            var dt = await _db.ExecuteQueryAsync(query, parameters);
            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return new Section
            {
                SectionId = Convert.ToInt32(row["section_id"]),
                SectionName = row["section_name"].ToString() ?? "",
                ProgramId = row["program_id"] != DBNull.Value ? Convert.ToInt32(row["program_id"]) : 0,
                YearLevel = row["year_level"] != DBNull.Value ? Convert.ToInt32(row["year_level"]) : 0,
                SectionCreatedAt = Convert.ToDateTime(row["section_created_at"])
            };
        }

        public async Task<Section?> GetByNameAsync(string sectionName)
        {
            string query = "SELECT * FROM sections WHERE section_name = @name;";
            var parameters = new Dictionary<string, object> { { "@name", sectionName } };

            var dt = await _db.ExecuteQueryAsync(query, parameters);
            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return new Section
            {
                SectionId = Convert.ToInt32(row["section_id"]),
                SectionName = row["section_name"].ToString() ?? "",
                ProgramId = row["program_id"] != DBNull.Value ? Convert.ToInt32(row["program_id"]) : 0,
                YearLevel = row["year_level"] != DBNull.Value ? Convert.ToInt32(row["year_level"]) : 0,
                SectionCreatedAt = Convert.ToDateTime(row["section_created_at"])
            };
        }

        public async Task<bool> AddAsync(Section section)
        {
            string query = @"
                INSERT INTO sections (section_name, program_id, year_level, section_created_at)
                VALUES (@name, @program, @year, @created);";

            var parameters = new Dictionary<string, object>
            {
                { "@name", section.SectionName },
                { "@program", section.ProgramId },
                { "@year", section.YearLevel },
                { "@created", section.SectionCreatedAt }
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> UpdateAsync(Section section)
        {
            string query = @"
                UPDATE sections SET
                    section_name = @name,
                    program_id = @program,
                    year_level = @year
                WHERE section_id = @id;";

            var parameters = new Dictionary<string, object>
            {
                { "@id", section.SectionId },
                { "@name", section.SectionName },
                { "@program", section.ProgramId },
                { "@year", section.YearLevel }
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> DeleteAsync(int sectionId)
        {
            string query = "DELETE FROM sections WHERE section_id = @id;";
            var parameters = new Dictionary<string, object> { { "@id", sectionId } };
            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> ExistsAsync(int sectionId)
        {
            string query = "SELECT COUNT(*) FROM sections WHERE section_id = @id;";
            var parameters = new Dictionary<string, object> { { "@id", sectionId } };
            return await _db.RecordExistsAsync(query, parameters);
        }

        // Optimized DTO methods
        public async Task<IEnumerable<SectionListDto>> GetAllWithDetailsAsync()
        {
            string query = @"
                SELECT 
                    s.section_id,
                    s.section_name,
                    s.program_id,
                    s.year_level,
                    s.section_created_at,
                    COALESCE(p.program_name, 'N/A') AS program_name
                FROM sections s
                LEFT JOIN programs p ON s.program_id = p.program_id
                ORDER BY s.section_id;";

            var dt = await _db.ExecuteQueryAsync(query);

            return dt.AsEnumerable().Select(row => new SectionListDto
            {
                SectionId = Convert.ToInt32(row["section_id"]),
                SectionName = row["section_name"]?.ToString() ?? "",
                ProgramId = row["program_id"] != DBNull.Value ? Convert.ToInt32(row["program_id"]) : 0,
                ProgramName = row["program_name"]?.ToString() ?? "N/A",
                YearLevel = row["year_level"] != DBNull.Value ? Convert.ToInt32(row["year_level"]) : (int?)null,
                SectionCreatedAt = Convert.ToDateTime(row["section_created_at"])
            });
        }

        public async Task<SectionDetailDto?> GetDetailByIdAsync(int sectionId)
        {
            string query = @"
                SELECT 
                    s.section_id,
                    s.section_name,
                    s.program_id,
                    s.year_level,
                    s.section_created_at,
                    p.program_name,
                    p.program_code,
                    p.dept_id,
                    d.dept_name
                FROM sections s
                LEFT JOIN programs p ON s.program_id = p.program_id
                LEFT JOIN departments d ON p.dept_id = d.dept_id
                WHERE s.section_id = @id
                LIMIT 1;";

            var parameters = new Dictionary<string, object> { { "@id", sectionId } };
            var dt = await _db.ExecuteQueryAsync(query, parameters);

            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return new SectionDetailDto
            {
                SectionId = Convert.ToInt32(row["section_id"]),
                SectionName = row["section_name"]?.ToString() ?? "",
                ProgramId = row["program_id"] != DBNull.Value ? Convert.ToInt32(row["program_id"]) : 0,
                ProgramName = row["program_name"]?.ToString() ?? "",
                ProgramCode = row["program_code"] != DBNull.Value ? row["program_code"]?.ToString() : null,
                YearLevel = row["year_level"] != DBNull.Value ? Convert.ToInt32(row["year_level"]) : (int?)null,
                DeptId = row["dept_id"] != DBNull.Value ? Convert.ToInt32(row["dept_id"]) : (int?)null,
                DeptName = row["dept_name"] != DBNull.Value ? row["dept_name"]?.ToString() : null,
                SectionCreatedAt = Convert.ToDateTime(row["section_created_at"])
            };
        }
    }
}
