using System.Data;
using DbUp;
using LmMobileApi.Shared.Data;
using Microsoft.Data.SqlClient;
using System.Data.Common;


namespace LmMobileApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // **YENİ: Database bağlantısı ve migration**

            // 1. Connection string kontrolü
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            // 2. Database var mı kontrol et (mevcut DB için)
            EnsureDatabase.For.SqlDatabase(connectionString);

            // 3. DbUp konfigürasyonu (şimdilik script yok, sadece hazırlık)
            var upgrader = DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(typeof(Program).Assembly)
                .LogToConsole()
                .Build();

            var result = upgrader.PerformUpgrade();
            if (!result.Successful)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Database upgrade failed: {result.Error}");
                Console.ResetColor();
#if DEBUG
                Console.ReadLine();
#endif
                return;
            }

            // 4. Database servisleri DI'ya kaydet
            builder.Services.AddScoped<IDbConnection>(sp =>
                new SqlConnection(connectionString));

            builder.Services.AddScoped<IDatabaseContext, DapperDatabaseContext>();
            builder.Services.AddScoped<IUnitOfWork, DapperUnitOfWork>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // Test endpoints
            app.MapGet("/api/test", () => "LmMobileApi çalışıyor! 🚀")
                .WithName("TestEndpoint")
                .WithTags("Test");

            app.MapGet("/api/test/database", async (IDbConnection connection) =>
            {
                try
                {
                    var dbConn = (DbConnection)connection;
                    await dbConn.OpenAsync();
                    return Results.Ok(new { success = true, message = "Database bağlantısı başarılı! ✅" });
                }
                catch (Exception ex)
                {
                    return Results.BadRequest(new { success = false, error = ex.Message });
                }
            })
                .WithName("TestDatabase")
            .WithTags("Test");


            app.Run();
        }
    }
}