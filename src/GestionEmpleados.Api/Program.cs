using System.Text.Json.Serialization;
using GestionEmpleados.Api.Extensions;
using GestionEmpleados.Api.Middleware;
using GestionEmpleados.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Límite general bajo; los endpoints con fotografía lo elevan con [RequestSizeLimit].
builder.WebHost.ConfigureKestrel(options =>
    options.Limits.MaxRequestBodySize = ServiceCollectionExtensions.LimiteSolicitudGeneralBytes);

builder.Services
    .AddControllers(options => options.SuppressAsyncSuffixInActionNames = false)
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

builder.Services.AddApplication(builder.Configuration);
builder.Services.AddInfrastructure();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStatusCodePages();
app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    // Interfaz interactiva para explorar y probar la API: /scalar/v1
    app.MapScalarApiReference(options => options.WithTitle("GestionEmpleados API"));
}

app.MapControllers();

await app.RunAsync();

public partial class Program;
