using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations
{
    public class AcademicTermRepository : IAcademicTermRepository
    {
        private readonly DatabaseHelper _db;

        public AcademicTermRepository(DatabaseHelper db)
        {
            _db = db;
        }

        public async Task<IEnumerable<AcademicTerm>> GetAllAsync()
        {
            string query = "SELECT * FROM academic_terms ORDER BY term_id;";
            var dt = await _db.ExecuteQueryAsync(query);

            return dt.AsEnumerable().Select(row => new AcademicTerm
            {
                TermId = Convert.ToInt32(row["term_id"]),
                TermName = row["term_name"].ToString() ?? "",
                StartDate = Convert.ToDateTime(row["start_date"]),
                EndDate = Convert.ToDateTime(row["end_date"]),
                IsActive = Convert.ToBoolean(row["is_active"])
            });
        }

        public async Task<AcademicTerm?> GetByIdAsync(int termId)
        {
            string query = "SELECT * FROM academic_terms WHERE term_id = @id;";
            var parameters = new Dictionary<string, object> { { "@id", termId } };

            var dt = await _db.ExecuteQueryAsync(query, parameters);
            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return new AcademicTerm
            {
                TermId = Convert.ToInt32(row["term_id"]),
                TermName = row["term_name"].ToString() ?? "",
                StartDate = Convert.ToDateTime(row["start_date"]),
                EndDate = Convert.ToDateTime(row["end_date"]),
                IsActive = Convert.ToBoolean(row["is_active"])
            };
        }

        public async Task<AcademicTerm?> GetByNameAsync(string termName)
        {
            string query = "SELECT * FROM academic_terms WHERE term_name = @name;";
            var parameters = new Dictionary<string, object> { { "@name", termName } };

            var dt = await _db.ExecuteQueryAsync(query, parameters);
            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return new AcademicTerm
            {
                TermId = Convert.ToInt32(row["term_id"]),
                TermName = row["term_name"].ToString() ?? "",
                StartDate = Convert.ToDateTime(row["start_date"]),
                EndDate = Convert.ToDateTime(row["end_date"]),
                IsActive = Convert.ToBoolean(row["is_active"])
            };
        }

        public async Task<AcademicTerm?> GetActiveTermAsync()
        {
            string query = "SELECT * FROM academic_terms WHERE is_active = TRUE LIMIT 1;";
            var dt = await _db.ExecuteQueryAsync(query);

            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return new AcademicTerm
            {
                TermId = Convert.ToInt32(row["term_id"]),
                TermName = row["term_name"].ToString() ?? "",
                StartDate = Convert.ToDateTime(row["start_date"]),
                EndDate = Convert.ToDateTime(row["end_date"]),
                IsActive = true
            };
        }

        public async Task<bool> AddAsync(AcademicTerm term)
        {
            string query = @"
                INSERT INTO academic_terms (term_name, start_date, end_date, is_active)
                VALUES (@name, @start, @end, @active);";

            var parameters = new Dictionary<string, object>
            {
                { "@name", term.TermName },
                { "@start", term.StartDate },
                { "@end", term.EndDate },
                { "@active", term.IsActive }
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> UpdateAsync(AcademicTerm term)
        {
            string query = @"
                UPDATE academic_terms SET
                    term_name = @name,
                    start_date = @start,
                    end_date = @end,
                    is_active = @active
                WHERE term_id = @id;";

            var parameters = new Dictionary<string, object>
            {
                { "@id", term.TermId },
                { "@name", term.TermName },
                { "@start", term.StartDate },
                { "@end", term.EndDate },
                { "@active", term.IsActive }
            };

            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> DeleteAsync(int termId)
        {
            string query = "DELETE FROM academic_terms WHERE term_id = @id;";
            var parameters = new Dictionary<string, object> { { "@id", termId } };
            return await _db.ExecuteNonQueryAsync(query, parameters) > 0;
        }

        public async Task<bool> SetActiveTermAsync(int termId)
        {
            var commands = new List<(string, Dictionary<string, object>?)>
            {
                ("UPDATE academic_terms SET is_active = FALSE;", null),
                ("UPDATE academic_terms SET is_active = TRUE WHERE term_id = @id;", new Dictionary<string, object> { { "@id", termId } })
            };

            return await _db.ExecuteTransactionAsync(commands);
        }

        public async Task<bool> ExistsAsync(int termId)
        {
            string query = "SELECT COUNT(*) FROM academic_terms WHERE term_id = @id;";
            var parameters = new Dictionary<string, object> { { "@id", termId } };
            return await _db.RecordExistsAsync(query, parameters);
        }
    }
}
