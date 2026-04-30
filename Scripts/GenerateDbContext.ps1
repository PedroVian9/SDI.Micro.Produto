$ErrorActionPreference = "Stop"

function Generate-DbContext {
    param (
        [string]$ProjectName = "Alta.Back.API"
    )

    $targetDir = Join-Path $PSScriptRoot "..\Alta.Back.API\Data"
    if (-not (Test-Path $targetDir)) {
        New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
    }

    $targetFile = Join-Path $targetDir "ApplicationDbContext.cs"
    
    $content = @"
using Serilog;
using Alta.Back.Lib.Helpers;
using Alta.Back.Lib.Constants;
using Microsoft.EntityFrameworkCore;
// using GavTech.Base.ClienteApi.Models; // Placeholder for future entities
// using GavTech.Base.ClienteApi.Models.Entity; // Placeholder
using Microsoft.Extensions.Configuration; // Required for IConfiguration

namespace $ProjectName.Data
{
    public class ApplicationDbContext : DbContext
    {
        protected readonly IConfiguration _configuration;
        private readonly IServiceProvider _services;

        public ApplicationDbContext(IConfiguration configuration, IServiceProvider services, DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            _configuration = configuration;
            _services = services;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (options.IsConfigured) return;

            try
            {
                var host = Environment.GetEnvironmentVariable("POSTGRESQL_HOST");
                var port = Environment.GetEnvironmentVariable("POSTGRESQL_PORT");
                var usuario = Environment.GetEnvironmentVariable("POSTGRESQL_USER");
                var password = Environment.GetEnvironmentVariable("POSTGRESQL_PASSWORD");
                var database = Environment.GetEnvironmentVariable("POSTGRESQL_DATABASE");

                string stringDeConexao = string.Empty;
                var defaultConnection = _configuration.GetConnectionString("DefaultConnection") ?? string.Empty;

                if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(password))
                {
                    stringDeConexao = defaultConnection;
                }
                else if (!string.IsNullOrEmpty(defaultConnection))
                {
                    stringDeConexao = string.Format(defaultConnection, host, port, database, usuario, password);
                }
                
                if (string.IsNullOrEmpty(stringDeConexao)) return;

                options.UseNpgsql(stringDeConexao, x => x.MigrationsHistoryTable("__EFMigrationsHistory", "public"));
            }
            catch(Exception ex)
            {
                Log.Error(ex, "Erro ao conectar no banco de dados.");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasDefaultSchema("public");

            // Examples of relationships and seeds configuration
            /*
            modelBuilder.Entity<MyEntity>()
                        .ToTable("MyTable", "MySchema");
            */
        }
        
        // public DbSet<MyEntity> MyEntities { get; set; }
    }
}
"@

    Set-Content -Path $targetFile -Value $content
    Write-Host "DbContext created at $targetFile"
}

Generate-DbContext
