using System;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;

namespace GoodGovernanceApp.Data
{
    public class DatabaseHelper
    {
        public async Task<MySqlConnection> OpenConnectionAsync()
        {
            MySqlConnection connection = new MySqlConnection(DatabaseConfig.HostingerConnectionString);
            await connection.OpenAsync();
            return connection;
        }

        public async Task<(bool IsSuccess, string Message)> TestConnectionAsync(string? connectionStringOverride = null)
        {
            try
            {
                string connectionString = connectionStringOverride ?? DatabaseConfig.ConnectionString;
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    return (true, "Connection successful!");
                }
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

        public async Task<DataTable> ExecuteQueryAsync(string query, params MySqlParameter[] parameters)
        {
            using (var connection = await OpenConnectionAsync())
            {
                using (var command = new MySqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    using (var adapter = new MySqlDataAdapter(command))
                    {
                        DataTable dataTable = new DataTable();
                        await Task.Run(() => adapter.Fill(dataTable));
                        return dataTable;
                    }
                }
            }
        }

        public async Task<int> ExecuteNonQueryAsync(string query, params MySqlParameter[] parameters)
        {
            using (var connection = await OpenConnectionAsync())
            {
                using (var command = new MySqlCommand(query, connection))
                {
                    if (parameters != null)
                    {
                        command.Parameters.AddRange(parameters);
                    }

                    return await command.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<object?> ExecuteScalarAsync(string query, params MySqlParameter[] parameters)
        {
            using (var connection = await OpenConnectionAsync())
            {
                using (var command = new MySqlCommand(query, connection))
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
