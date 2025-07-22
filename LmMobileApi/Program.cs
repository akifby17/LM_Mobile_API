using DbUp;
using LmMobileApi.Operations.Application.Services;
using LmMobileApi.Operations.Infrastructure;
using LmMobileApi.Personnels.Application.Services;
using LmMobileApi.Personnels.Infrastructure.Repositories;
using LmMobileApi.Shared.Data;
using LmMobileApi.Shared.Endpoints;
using LmMobileApi.Users.Application.Services;
using LmMobileApi.Users.Domain;
using LmMobileApi.Users.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Data;
using System.Text;
using LmMobileApi.Operations.Infrastructure;
using LmMobileApi.Operations.Application.Services;


namespace LmMobileApi
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddEndpointsApiExplorer();

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


            builder.Services.AddAuthorization();


            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(corsPolicyBuilder =>
                {
                    corsPolicyBuilder.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader();
                });
            });
            // User servisleri
            builder.Services.AddScoped<IUserRepository, UserRepository>();
    

            // GÜNCEL:
            builder.Services.AddScoped<IUserService>(provider =>
            {
                var userRepository = provider.GetRequiredService<IUserRepository>();
                var refreshTokenRepository = provider.GetRequiredService<IRefreshTokenRepository>();
                var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
                return new UserService(userRepository, refreshTokenRepository, unitOfWork);
            });

            builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
          
            builder.Services.AddHostedService<RefreshTokenCleanupService>();

            builder.Services.AddScoped<IPersonnelRepository, PersonnelRepository>();
            builder.Services.AddScoped<IPersonnelService, PersonnelService>();

            // **YENİ: Operations servisleri**
            builder.Services.AddScoped<IOperationRepository, OperationRepository>();
            builder.Services.AddScoped<IOperationService, OperationService>();


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
            builder.Services.AddScoped<IDbConnection>(sp =>
                new SqlConnection(connectionString));

            builder.Services.AddScoped<IDatabaseContext, DapperDatabaseContext>();
            builder.Services.AddScoped<IUnitOfWork, DapperUnitOfWork>();

            
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IUserService, UserService>();

            
            builder.Services.AddEndpoints(typeof(Program).Assembly);

            var app = builder.Build();

          
            app.UseAuthentication();
            app.UseAuthorization();

            
            app.UseCors();

            
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

           
            app.MapEndpoints();

            app.Run();
        }
    }
}