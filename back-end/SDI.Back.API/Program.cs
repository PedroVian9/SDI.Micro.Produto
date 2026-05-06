using Dapper;
using Npgsql;
using SDI.Back.API.Data;
using SDI.Back.API.HealthChecks;
using SDI.Back.API.Messaging;
using SDI.Back.API.Middlewares;
using SDI.Back.API.Repositories;
using SDI.Back.API.Repositories.Interfaces;
using SDI.Back.API.Services;
using SDI.Back.API.Services.Interfaces;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
EnvFileLoader.LoadFromSolutionOrProjectRoot(builder.Environment.ContentRootPath);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks()
    .AddCheck<PostgresHealthCheck>("postgres");
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddKafkaMessaging(builder.Configuration);

DefaultTypeMap.MatchNamesWithUnderscores = true;

builder.Services.AddSingleton(sp =>
{
    var connectionString = PostgresConnectionStringResolver.Resolve(sp.GetRequiredService<IConfiguration>());
    return NpgsqlDataSource.Create(connectionString);
});
builder.Services.AddScoped<IDbConnectionFactory, NpgsqlConnectionFactory>();

builder.Services.AddScoped<ITransporteRepository, TransporteRepository>();
builder.Services.AddScoped<ICategoriaRepository, CategoriaRepository>();
builder.Services.AddScoped<IUnidadeMedidaRepository, UnidadeMedidaRepository>();
builder.Services.AddScoped<IProdutoRepository, ProdutoRepository>();

builder.Services.AddScoped<ITransporteService, TransporteService>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IUnidadeMedidaService, UnidadeMedidaService>();
builder.Services.AddScoped<IProdutoService, ProdutoService>();

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

var app = builder.Build();

app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
