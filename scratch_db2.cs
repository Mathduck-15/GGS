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
            cmd.Parameters.AddWithValue("@pid", "TEST-2026-0001");
            cmd.Parameters.AddWithValue("@project", "Test Project");
            cmd.Parameters.AddWithValue("@desc", DBNull.Value);
            cmd.Parameters.AddWithValue("@code", "OFFICE-A");
            cmd.Parameters.AddWithValue("@budget", 1000.50m);
            cmd.Parameters.AddWithValue("@contact", "John Doe");
            cmd.Parameters.AddWithValue("@ybid", 1);
            cmd.Parameters.AddWithValue("@voucher", "VOUCHER123");

            int rows = await cmd.ExecuteNonQueryAsync();
            Console.WriteLine($"Inserted rows: {rows}");
            
            // Check it
            const string checkSql = "SELECT COUNT(*) FROM project_details WHERE project_details_id = @pid;";
            using var cmd2 = new MySqlCommand(checkSql, conn);
            cmd2.Parameters.AddWithValue("@pid", "TEST-2026-0001");
            var count = await cmd2.ExecuteScalarAsync();
            Console.WriteLine($"Count: {count}");

        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.ToString());
        }
    }
}
