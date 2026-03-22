using Microsoft.EntityFrameworkCore;
using HotelManagement.Datos.Config;
using HotelManagement.Repositories;
using HotelManagement.Services;
using HotelManagement.Aplicacion.Validators;
using HotelManagement.Presentacion.Middleware;
using HotelManagement.Application.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using HotelManagement.Datos.Repositories;
using DotNetEnv;

var rootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
var envPath = Path.Combine(rootPath, ".env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
    Console.WriteLine($"[INFO] Archivo .env cargado desde: {envPath}");
}
else
{
    Console.WriteLine($"[WARN] Archivo .env no encontrado en: {envPath}");
}

var builder = WebApplication.CreateBuilder(args);

var server = Environment.GetEnvironmentVariable("DB_SERVER") ?? "localhost";
var port = Environment.GetEnvironmentVariable("PORT") ?? Environment.GetEnvironmentVariable("DB_PORT") ?? "3306";
var database = Environment.GetEnvironmentVariable("DB_NAME") ?? "HotelDB";
var user = Environment.GetEnvironmentVariable("DB_USER") ?? "root";
var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "";
Console.WriteLine($"[INFO] Conectando a MySQL en {server}:{port} como {user}");

var connectionString = $"Server={server};Port={port};Database={database};User={user};Password={password};";

// Configurar DbContext
builder.Services.AddDbContext<HotelDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Registrar servicios
builder.Services.AddScoped<IDetalleReservaRepository, DetalleReservaRepository>();
builder.Services.AddScoped<IDetalleReservaService, DetalleReservaService>();
builder.Services.AddScoped<IDetalleReservaValidator, DetalleReservaValidator>();



builder.Services.AddScoped<IReservaRepository, ReservaRepository>();
builder.Services.AddScoped<IReservaService, ReservaService>();
builder.Services.AddControllers();
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();



//Cliente
builder.Services.AddScoped<IClienteRepository, ClienteRepository>();
builder.Services.AddScoped<IClienteService, ClienteService>(); 
builder.Services.AddScoped<IClienteValidator, ClienteValidator>();

//Huesped
builder.Services.AddScoped<IHuespedRepository, HuespedRepository>();
builder.Services.AddScoped<HotelManagement.Aplicacion.Validators.IHuespedValidator, HotelManagement.Aplicacion.Validators.HuespedValidator>();
builder.Services.AddScoped<IHuespedService, HuespedService>();

//Habitacion
builder.Services.AddScoped<IHabitacionRepository, HabitacionRepository>();
builder.Services.AddScoped<HotelManagement.Aplicacion.Validators.IHabitacionValidator, HotelManagement.Aplicacion.Validators.HabitacionValidator>();

// Configurar controladores
builder.Services.AddControllers();

// Configurar Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Hotel Management API",
        Version = "v1",
        Description = "API para la gestión de reservas de hotel",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "Hotel Management Team"
        }
    });
});

// Configurar CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Middleware de manejo de errores
app.UseMiddleware<ErrorHandlingMiddleware>();

// Health check endpoint para CI/CD
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Configurar pipeline HTTP
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hotel Management API v1");
    c.RoutePrefix = string.Empty;
});
app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();

