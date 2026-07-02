using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using MySqlConnector;

namespace GoodGovernanceApp.Data
{
    public class DatabaseHelper
    {
        private string GetLocalConnectionString()
        {
            string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GoodGovernanceApp");
            return $"Data Source={Path.Combine(appDataFolder, "ggms.db")}";
        }

        public async Task<SqliteConnection> OpenConnectionAsync()
        {
            SqliteConnection connection = new SqliteConnection(GetLocalConnectionString());
            await connection.OpenAsync();
            return connection;
        }

        public async Task<(bool IsSuccess, string Message)> TestConnectionAsync(string? connectionStringOverride = null)
        {
            try
            {
                // This is generally testing the remote connection from SettingsViewModel
                if (!string.IsNullOrEmpty(connectionStringOverride) && connectionStringOverride.Contains("Server="))
                {
                    using (var connection = new MySqlConnection(connectionStringOverride))
                    {
                        await connection.OpenAsync();
                        return (true, "Cloud connection successful!");
                    }
                }
                
                string connectionString = connectionStringOverride ?? GetLocalConnectionString();
                using (var connection = new SqliteConnection(connectionString))
                {
                    await connection.OpenAsync();
                    return (true, "Local connection successful!");
                }
            }
            catch (SqliteException ex)
            {
                return (false, $"SQLite Error [{ex.SqliteErrorCode}]: {ex.Message}");
            }
            catch (MySqlException ex)
            {
                return (false, $"MySQL Error [{ex.Number}]: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Connection Error: {ex.Message}");
            }
        }

        public async Task<DataTable> ExecuteQueryAsync(string query, params SqliteParameter[] parameters)
        {
            using (var connection = await OpenConnectionAsync())
            {
                using (var command = new SqliteCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        DataTable dataTable = new DataTable();
                        dataTable.Load(reader);
                        return dataTable;
                    }
                }
            }
        }

        public async Task<int> ExecuteNonQueryAsync(string query, params SqliteParameter[] parameters)
        {
            using (var connection = await OpenConnectionAsync())
            {
                using (var command = new SqliteCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    return await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<object?> ExecuteScalarAsync(string query, params SqliteParameter[] parameters)
        {
            using (var connection = await OpenConnectionAsync())
            {
                using (var command = new SqliteCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    var result = await command.ExecuteScalarAsync();
                    return result == DBNull.Value ? null : result;
                }
            }
        }
    }
}
