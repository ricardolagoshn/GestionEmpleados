using GestionEmpleados.Application.Common.Options;
using GestionEmpleados.Application.Interfaces;
using GestionEmpleados.Application.Services;

namespace GestionEmpleados.Api.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>Límite general del cuerpo de las solicitudes.</summary>
    public const long LimiteSolicitudGeneralBytes = 1 * 1024 * 1024;

    /// <summary>Límite para endpoints que reciben fotografía: 5 MB en Base64 (~6.7 MB) más el JSON.</summary>
    public const long LimiteSolicitudFotografiaBytes = 8 * 1024 * 1024;

    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        var opciones = configuration.GetSection(EmpleadoOptions.SectionName).Get<EmpleadoOptions>() ?? new EmpleadoOptions();

        if (opciones.EdadMinima < 0)
        {
            throw new InvalidOperationException($"{EmpleadoOptions.SectionName}:EdadMinima no puede ser negativa.");
        }

        if (opciones.FotografiaMaxBytes is <= 0 or > 6 * 1024 * 1024)
        {
            throw new InvalidOperationException($"{EmpleadoOptions.SectionName}:FotografiaMaxBytes debe estar entre 1 y 6291456.");
        }

        services.AddSingleton(opciones);
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IEmpleadoService, EmpleadoService>();
        services.AddScoped<IPaisService, PaisService>();

        return services;
    }
}
