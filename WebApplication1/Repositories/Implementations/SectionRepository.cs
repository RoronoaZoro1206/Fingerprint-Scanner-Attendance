using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
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
    }
}
