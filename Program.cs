using System.Net.Http.Json;
using Microsoft.Data.SqlClient;

Console.WriteLine($"[{DateTime.UtcNow:u}] Starting External API Employee Sync Job...");

var connString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
    ?? throw new InvalidOperationException("Missing ConnectionStrings__DefaultConnection");

// 1. Pull data from an External REST API
using var http = new HttpClient();
var externalUsers = await http.GetFromJsonAsync<List<ExternalUser>>("https://typicode.com") ?? [];

Console.WriteLine($"Fetched {externalUsers.Count} users from external API.");

// 2. Insert new employees into Azure SQL (Idempotent check)
await using var conn = new SqlConnection(connString);
await conn.OpenAsync();

int inserted = 0;
foreach (var user in externalUsers.Take(3))
{
    var syncName = $"{user.Name} (API Sync)";

    await using var checkCmd = new SqlCommand(
        "SELECT COUNT(1) FROM [dbo].[EmployeeDetails] WHERE [Name] = @Name", conn);
    checkCmd.Parameters.AddWithValue("@Name", syncName);

    var exists = (int)(await checkCmd.ExecuteScalarAsync() ?? 0) > 0;
    if (!exists)
    {
        await using var insertCmd = new SqlCommand(
            "INSERT INTO [dbo].[EmployeeDetails] ([Name], [DepartmentId]) VALUES (@Name, 1)", conn);
        insertCmd.Parameters.AddWithValue("@Name", syncName);
        await insertCmd.ExecuteNonQueryAsync();
        inserted++;
    }
}

Console.WriteLine($"[{DateTime.UtcNow:u}] Sync complete. Inserted {inserted} new employees.");

record ExternalUser(int Id, string Name, string Email);
