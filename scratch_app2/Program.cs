using System;
using MySqlConnector;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        string connStr = "Server=127.0.0.1;Port=3306;Database=govern;User=root;Password=root123;SslMode=None;AllowPublicKeyRetrieval=True;";
        try
        {
            using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync();
            
            const string insertSql = @"
            INSERT INTO project_details
                (project_details_id, project, description, office_code, total_budget, contact_person, yearly_budget_id, create_at, updated_at, voucher_code)
            VALUES
                (@pid, @project, @desc, @code, @budget, @contact, @ybid, NOW(), NOW(), @voucher);";

            using var cmd = new MySqlCommand(insertSql, conn);
            cmd.Parameters.AddWithValue("@pid", "TEST-2026-0002");
            cmd.Parameters.AddWithValue("@project", "Test Project 2");
            cmd.Parameters.AddWithValue("@desc", DBNull.Value);
            cmd.Parameters.AddWithValue("@code", "OFFICE-A");
            cmd.Parameters.AddWithValue("@budget", 1000.50m);
            cmd.Parameters.AddWithValue("@contact", "John Doe");
            cmd.Parameters.AddWithValue("@ybid", DBNull.Value); // Put DBNull!
            cmd.Parameters.AddWithValue("@voucher", "VOUCHER124");

            int rows = await cmd.ExecuteNonQueryAsync();
            Console.WriteLine($"Inserted rows: {rows}");

        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.ToString());
        }
    }
}
