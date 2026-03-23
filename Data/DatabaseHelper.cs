using System;
using System.Data;
using System.Threading.Tasks;
using MySqlConnector;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GoodGovernanceApp.Data
{
    public class DatabaseHelper
    {
        private readonly IConfiguration _configuration;

        public DatabaseHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString()
        {
            string dbMode = _configuration["AppSettings:DatabaseMode"] ?? "Local";
            string connectionStringName = dbMode switch
            {
                "Remote" => "RemoteConnection",
                "LAN" => "LanConnection",
                _ => "LocalConnection"
            };
            return _configuration.GetConnectionString(connectionStringName) ?? throw new InvalidOperationException($"Connection string '{connectionStringName}' not found.");
        }

        public async Task<MySqlConnection> OpenConnectionAsync()
        {
            string connectionString = GetConnectionString();
            MySqlConnection connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();
            return connection;
        }

        public async Task<(bool IsSuccess, string Message)> TestConnectionAsync(string? connectionStringOverride = null)
        {
            try
            {
                string connectionString = connectionStringOverride ?? GetConnectionString();
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    return (true, "Connection to Hostinger remote database successful!");
                }
            }
            catch (MySqlException ex)
            {
                // Providing explicit troubleshooting for Hostinger if it fails
                return (false, $"MySQL Error [{ex.Number}]: {ex.Message}\n\nTroubleshooting:\n1. Ensure your current IP is added to the 'Remote MySQL' section in Hostinger hPanel.\n2. Ensure the remote database user has full privileges.");
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
