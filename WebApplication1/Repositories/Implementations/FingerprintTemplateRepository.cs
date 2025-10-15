using System.Data;
using Fingerprint_Attendance_System.Data;
using Fingerprint_Attendance_System.Models;
using Fingerprint_Attendance_System.Repositories.Interfaces;

namespace Fingerprint_Attendance_System.Repositories.Implementations
{
    public class FingerprintTemplateRepository : IFingerprintTemplateRepository
    {
        private readonly DatabaseHelper _db;

        public FingerprintTemplateRepository(DatabaseHelper db)
        {
            _db = db;
        }

        public async Task<bool> HasFingerprintAsync(int userId)
        {
            var query = @"
                SELECT COUNT(*) 
                FROM fingerprint_templates 
                WHERE user_id = @user_id";

            var parameters = new Dictionary<string, object>
            {
                { "@user_id", userId }
            };

            var result = await _db.ExecuteQueryAsync(query, parameters);
            
            if (result.Rows.Count > 0)
            {
                var count = Convert.ToInt32(result.Rows[0][0]);
                return count > 0;
            }

            return false;
        }

        public async Task<FingerprintTemplate?> GetByUserIdAsync(int userId)
        {
            var query = @"
                SELECT ftemp_id, user_id, ftemp_data, ftemp_name, ftemp_created_at
                FROM fingerprint_templates
                WHERE user_id = @user_id";

            var parameters = new Dictionary<string, object>
            {
                { "@user_id", userId }
            };

            var result = await _db.ExecuteQueryAsync(query, parameters);

            if (result.Rows.Count == 0)
                return null;

            var row = result.Rows[0];
            return new FingerprintTemplate
            {
                FtempId = Convert.ToInt32(row["ftemp_id"]),
                UserId = Convert.ToInt32(row["user_id"]),
                FtempData = row["ftemp_data"] as byte[] ?? Array.Empty<byte>(),
                FtempName = row["ftemp_name"] == DBNull.Value ? null : row["ftemp_name"].ToString(),
                FtempCreatedAt = Convert.ToDateTime(row["ftemp_created_at"])
            };
        }

        public async Task<FingerprintTemplate?> GetByIdAsync(int ftempId)
        {
            var query = @"
                SELECT ftemp_id, user_id, ftemp_data, ftemp_name, ftemp_created_at
                FROM fingerprint_templates
                WHERE ftemp_id = @ftemp_id";

            var parameters = new Dictionary<string, object>
            {
                { "@ftemp_id", ftempId }
            };

            var result = await _db.ExecuteQueryAsync(query, parameters);

            if (result.Rows.Count == 0)
                return null;

            var row = result.Rows[0];
            return new FingerprintTemplate
            {
                FtempId = Convert.ToInt32(row["ftemp_id"]),
                UserId = Convert.ToInt32(row["user_id"]),
                FtempData = row["ftemp_data"] as byte[] ?? Array.Empty<byte>(),
                FtempName = row["ftemp_name"] == DBNull.Value ? null : row["ftemp_name"].ToString(),
                FtempCreatedAt = Convert.ToDateTime(row["ftemp_created_at"])
            };
        }

        public async Task<int> CreateAsync(FingerprintTemplate template)
        {
            var query = @"
                INSERT INTO fingerprint_templates (user_id, ftemp_data, ftemp_name, ftemp_created_at)
                VALUES (@user_id, @ftemp_data, @ftemp_name, @ftemp_created_at)
                RETURNING ftemp_id";

            var parameters = new Dictionary<string, object>
            {
                { "@user_id", template.UserId },
                { "@ftemp_data", template.FtempData },
                { "@ftemp_name", template.FtempName ?? (object)DBNull.Value },
                { "@ftemp_created_at", template.FtempCreatedAt }
            };

            var result = await _db.ExecuteQueryAsync(query, parameters);

            if (result.Rows.Count > 0)
            {
                return Convert.ToInt32(result.Rows[0][0]);
            }

            throw new Exception("Failed to create fingerprint template");
        }

        public async Task<bool> UpdateAsync(FingerprintTemplate template)
        {
            var query = @"
                UPDATE fingerprint_templates
                SET ftemp_data = @ftemp_data,
                    ftemp_name = @ftemp_name
                WHERE ftemp_id = @ftemp_id";

            var parameters = new Dictionary<string, object>
            {
                { "@ftemp_id", template.FtempId },
                { "@ftemp_data", template.FtempData },
                { "@ftemp_name", template.FtempName ?? (object)DBNull.Value }
            };

            var result = await _db.ExecuteNonQueryAsync(query, parameters);
            return result > 0;
        }

        public async Task<bool> DeleteByUserIdAsync(int userId)
        {
            var query = @"
                DELETE FROM fingerprint_templates
                WHERE user_id = @user_id";

            var parameters = new Dictionary<string, object>
            {
                { "@user_id", userId }
            };

            var result = await _db.ExecuteNonQueryAsync(query, parameters);
            return result > 0;
        }

        public async Task<IEnumerable<FingerprintTemplate>> GetAllAsync()
        {
            var query = @"
                SELECT ftemp_id, user_id, ftemp_data, ftemp_name, ftemp_created_at
                FROM fingerprint_templates
                ORDER BY ftemp_created_at DESC";

            var result = await _db.ExecuteQueryAsync(query);
            var templates = new List<FingerprintTemplate>();

            foreach (DataRow row in result.Rows)
            {
                templates.Add(new FingerprintTemplate
                {
                    FtempId = Convert.ToInt32(row["ftemp_id"]),
                    UserId = Convert.ToInt32(row["user_id"]),
                    FtempData = row["ftemp_data"] as byte[] ?? Array.Empty<byte>(),
                    FtempName = row["ftemp_name"] == DBNull.Value ? null : row["ftemp_name"].ToString(),
                    FtempCreatedAt = Convert.ToDateTime(row["ftemp_created_at"])
                });
            }

            return templates;
        }
    }
}
