using Dapper;
using Npgsql;
using SDI.Back.Template.Data;
using SDI.Back.Template.HealthChecks;
using SDI.Back.Template.Messaging;
using SDI.Back.Template.Middlewares;
using SDI.Back.Template.Repositories;
using SDI.Back.Template.Repositories.Interfaces;
using SDI.Back.Template.Services;
using SDI.Back.Template.Services.Interfaces;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

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

if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
