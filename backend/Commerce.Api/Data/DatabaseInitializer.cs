using System.Data;
using System.Text.RegularExpressions;
using Commerce.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

namespace Commerce.Api.Data;

public static partial class DatabaseInitializer
{
    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex BatchSeparator();

    public static async Task InitializeAsync(CommerceRepository repository, IConfiguration configuration, IHostEnvironment environment)
    {
        var builder = new SqlConnectionStringBuilder(repository.ConnectionString);
        var database = builder.InitialCatalog;
        if (string.IsNullOrWhiteSpace(database)) throw new InvalidOperationException("La conexión debe especificar Database.");
        builder.InitialCatalog = "master";
        await using (var master = new SqlConnection(builder.ConnectionString))
        {
            await master.OpenAsync();
            await using var create = new SqlCommand("IF DB_ID(@name) IS NULL BEGIN DECLARE @statement NVARCHAR(MAX)=N'CREATE DATABASE ' + QUOTENAME(@name); EXEC sys.sp_executesql @statement; END", master);
            create.Parameters.Add("@name", SqlDbType.NVarChar, 128).Value = database;
            await create.ExecuteNonQueryAsync();
        }
        await using var connection = await repository.OpenAsync(CancellationToken.None);
        foreach (var file in Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "database"), "*.sql").Order())
            foreach (var batch in BatchSeparator().Split(await File.ReadAllTextAsync(file)).Where(b => !string.IsNullOrWhiteSpace(b)))
            {
                await using var command = new SqlCommand(batch, connection) { CommandTimeout = 60 };
                await command.ExecuteNonQueryAsync();
            }
        if (environment.IsDevelopment())
        {
            var email = configuration["DemoUser:Email"] ?? throw new InvalidOperationException("Falta DemoUser:Email.");
            var name = configuration["DemoUser:Name"] ?? "Usuario Demo";
            var password = configuration["DemoUser:Password"] ?? throw new InvalidOperationException("Falta DemoUser:Password.");
            var user = new AppUser(0, email, name, "");
            var hash = new PasswordHasher<AppUser>().HashPassword(user, password);
            await using var seed = new SqlCommand("IF NOT EXISTS(SELECT 1 FROM dbo.app_user WHERE email=@email) INSERT dbo.app_user(email,display_name,password_hash) VALUES(@email,@name,@hash)", connection);
            seed.Parameters.Add("@email", SqlDbType.NVarChar, 254).Value = email;
            seed.Parameters.Add("@name", SqlDbType.NVarChar, 100).Value = name;
            seed.Parameters.Add("@hash", SqlDbType.NVarChar, 500).Value = hash;
            await seed.ExecuteNonQueryAsync();
        }
        Console.WriteLine($"Base {database} inicializada. Scripts y procedimientos aplicados correctamente.");
    }
}
