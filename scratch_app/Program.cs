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
            Console.WriteLine("Connected!");

            using var cmd = new MySqlCommand("DESCRIBE project_details;", conn);
            using var reader = await cmd.ExecuteReaderAsync();
            Console.WriteLine("Table project_details:");
            while (await reader.ReadAsync())
            {
                Console.WriteLine($"{reader[0]} | {reader[1]} | {reader[2]} | {reader[3]} | {reader[4]}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.ToString());
        }
    }
}
