using System;
using System.IO;
using Npgsql;

class Program
{
    static void Main()
    {
        string dbConnStr = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
            ?? "Host=localhost;Port=5432;Database=bharattrustbank;Username=postgres;Password=your_password;";

        try
        {
            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            if (!File.Exists(Path.Combine(basePath, "Database_Postgres.sql")))
            {
                basePath = Path.GetFullPath(Path.Combine(basePath, "..", "..", ".."));
            }
            if (!File.Exists(Path.Combine(basePath, "Database_Postgres.sql")))
            {
                basePath = Directory.GetCurrentDirectory();
            }
            string[] files = { "Database_Postgres.sql", "SeedData_Postgres.sql" };

            using (var conn = new NpgsqlConnection(dbConnStr))
            {
                conn.Open();
                foreach (var file in files)
                {
                    string path = Path.Combine(basePath, file);
                    if (!File.Exists(path)) {
                        Console.WriteLine($"File not found: {path}");
                        continue;
                    }
                    
                    Console.WriteLine($"Executing {file}...");
                    string script = File.ReadAllText(path);
                    
                    try
                    {
                        var stmts = script.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var stmt in stmts) {
                            if (string.IsNullOrWhiteSpace(stmt)) continue;
                            try {
                                using (var cmd = new NpgsqlCommand(stmt + ";", conn))
                                {
                                    cmd.ExecuteNonQuery();
                                }
                            } catch (Exception ex) {
                                Console.WriteLine($"Error in statement: {stmt.Trim().Substring(0, Math.Min(50, stmt.Trim().Length))}... Error: {ex.Message}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in script {file}: {ex.Message}");
                    }
                }
            }
            Console.WriteLine("Database setup done.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception: " + ex.ToString());
        }
    }
}
