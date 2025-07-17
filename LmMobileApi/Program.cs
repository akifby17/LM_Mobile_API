using System.Data;
using DbUp;
using LmMobileApi.Shared.Data;
using LmMobileApi.Users.Infrastructure.Repositories;
using LmMobileApi.Users.Domain;
using Microsoft.Data.SqlClient;

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

            // Database bağlantısı ve migration
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            }

            EnsureDatabase.For.SqlDatabase(connectionString);

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

            // Database servisleri DI'ya kaydet
            builder.Services.AddScoped<IDbConnection>(sp =>
                new SqlConnection(connectionString));

            builder.Services.AddScoped<IDatabaseContext, DapperDatabaseContext>();
            builder.Services.AddScoped<IUnitOfWork, DapperUnitOfWork>();

            // User Repository
            builder.Services.AddScoped<IUserRepository, UserRepository>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }
            app.MapPost("/api/test/user", async (IUserRepository userRepository, string userName, string password) =>
            {
                try
                {
                    // Debug log
                    Console.WriteLine($"Test başladı: userName={userName}, password={password}");

                    // User oluştur
                    var user = new User(userName, password);
                    Console.WriteLine("User objesi oluşturuldu");

                    // Repository test
                    var repositoryResult = await userRepository.GetUserAsync(user);
                    Console.WriteLine($"Repository sonucu: IsSuccess={repositoryResult.IsSuccess}");

                    // Sonuç dön
                    if (repositoryResult.IsSuccess && repositoryResult.Data != null)
                    {
                        return Results.Ok(new
                        {
                            success = true,
                            user = new
                            {
                                userName = repositoryResult.Data.UserName,
                                personnelId = repositoryResult.Data.PersonnelId
                            },
                            message = "User authentication test successful! ✅"
                        });
                    }
                    else
                    {
                        return Results.BadRequest(new
                        {
                            success = false,
                            error = repositoryResult.Error.Code,
                            message = repositoryResult.Error.Description,
                            details = "User not found or authentication failed"
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex.Message}");
                    Console.WriteLine($"StackTrace: {ex.StackTrace}");

                    return Results.BadRequest(new
                    {
                        success = false,
                        error = "Exception",
                        message = ex.Message,
                        stackTrace = ex.StackTrace
                    });
                }
            })
            .WithName("TestUserAuth")
            .WithTags("Test");

            app.Run();
        }
    }
}