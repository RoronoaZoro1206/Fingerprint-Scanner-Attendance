using System.Data;
using Npgsql;

namespace Fingerprint_Attendance_System.Data
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper(IConfiguration config)
        {
            // Read connection string from appsettings.json
            _connectionString = config.GetConnectionString("DefaultConnection") 
       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration.");
        }

        // Get a new PostgreSQL connection
        private NpgsqlConnection GetConnection()
        {
     return new NpgsqlConnection(_connectionString);
        }

        // =========================================================
        // SELECT queries that return multiple rows (DataTable)
        // =========================================================
        public async Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object>? parameters = null)
        {
            using var conn = GetConnection();
            using var cmd = new NpgsqlCommand(query, conn);

            if (parameters != null)
            {
                foreach (var param in parameters)
                    cmd.Parameters.AddWithValue(param.Key, param.Value);
            }

            await conn.OpenAsync();

            using var reader = await cmd.ExecuteReaderAsync();
            var dt = new DataTable();
            dt.Load(reader);

            return dt;
        }

        // =========================================================
        // INSERT / UPDATE / DELETE queries
        // Returns number of affected rows
        // =========================================================
        public async Task<int> ExecuteNonQueryAsync(string query, Dictionary<string, object>? parameters = null)
        {
            using var conn = GetConnection();
            using var cmd = new NpgsqlCommand(query, conn);

            if (parameters != null)
            {
                foreach (var param in parameters)
                    cmd.Parameters.AddWithValue(param.Key, param.Value);
            }

            await conn.OpenAsync();
            return await cmd.ExecuteNonQueryAsync();
        }

        // =========================================================
        // Queries that return a single value (like COUNT, SUM, etc.)
        // =========================================================
        public async Task<object?> ExecuteScalarAsync(string query, Dictionary<string, object>? parameters = null)
        {
            using var conn = GetConnection();
            using var cmd = new NpgsqlCommand(query, conn);

            if (parameters != null)
            {
                foreach (var param in parameters)
                    cmd.Parameters.AddWithValue(param.Key, param.Value);
            }

            await conn.OpenAsync();
            return await cmd.ExecuteScalarAsync();
        }
        
        public async Task<bool> ExecuteTransactionAsync(List<(string Query, Dictionary<string, object>? Parameters)> commands)
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            using var tran = await conn.BeginTransactionAsync();

            try
            {
                foreach (var (query, parameters) in commands)
                {
                    using var cmd = new NpgsqlCommand(query, conn, tran);
                    if (parameters != null)
                    {
                        foreach (var param in parameters)
                            cmd.Parameters.AddWithValue(param.Key, param.Value);
                    }
                    await cmd.ExecuteNonQueryAsync();
                }

                await tran.CommitAsync();
                return true;
            }
            catch
            {
                await tran.RollbackAsync();
                return false;
            }
        }
        
        public async Task<bool> RecordExistsAsync(string query, Dictionary<string, object>? parameters = null)
        {
            var result = await ExecuteScalarAsync(query, parameters);
            if (result == null || result == DBNull.Value) return false;

            if (result is long l) return l > 0;
            if (result is int i) return i > 0;
            return false;
        }
        
        // =========================================================
        // DATABASE STATUS CHECK
        // =========================================================
        public async Task<bool> CheckDatabaseStatusAsync()
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync();
                using var cmd = new NpgsqlCommand("SELECT 1", conn);
                await cmd.ExecuteScalarAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

    }
}
