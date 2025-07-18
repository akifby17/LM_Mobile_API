using System.Data;
using System.Text;
using DbUp;
using LmMobileApi.Shared.Data;
using LmMobileApi.Shared.Endpoints;
using LmMobileApi.Users.Infrastructure.Repositories;
using LmMobileApi.Users.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace LmMobileApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddEndpointsApiExplorer();

            // **YENİ: Swagger + JWT Configuration**
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new() { Title = "Loom Monitoring Web Api", Version = "v1.0.0" });
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer"
                });
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            // **YENİ: JWT Authentication**
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = "Touchtech",
                        ValidAudience = "Touchtech",
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("Touchtech-Bilgi-Teknolojileri-Yazılım-Danismanlik")),
                    };
                });

            // **YENİ: Authorization**
            builder.Services.AddAuthorization();

            // **YENİ: CORS**
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(corsPolicyBuilder =>
                {
                    corsPolicyBuilder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });

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

            // **YENİ: User servisleri**
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IUserService, UserService>();

            // **YENİ: Endpoint sistemi**
            builder.Services.AddEndpoints(typeof(Program).Assembly);

            var app = builder.Build();

            // **YENİ: Authentication & Authorization middleware**
            app.UseAuthentication();
            app.UseAuthorization();

            // **YENİ: CORS**
            app.UseCors();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // **YENİ: Endpoints mapping**
            app.MapEndpoints();

            app.Run();
        }
    }
}