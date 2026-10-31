using GestionEmpleados.Application.Interfaces;
using GestionEmpleados.Infrastructure.Data;
using GestionEmpleados.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GestionEmpleados.Infrastructure;

public static class DependencyInjection
{
    public const string ConnectionStringName = "DefaultConnection";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // La cadena se lee al crear el contexto para respetar la configuración final del host (incluye pruebas).
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            var connectionString = serviceProvider.GetRequiredService<IConfiguration>().GetConnectionString(ConnectionStringName);
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"Falta la cadena de conexión '{ConnectionStringName}'. Configúrela con User Secrets o variables de entorno.");
            }

            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
        });

        services.AddScoped<IEmpleadoRepository, EmpleadoRepository>();
        services.AddScoped<IPaisRepository, PaisRepository>();

        return services;
    }
}
