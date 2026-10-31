using GestionEmpleados.Application.Common.Models;
using GestionEmpleados.Application.DTOs.Empleados;

namespace GestionEmpleados.Application.Interfaces;

public interface IEmpleadoService
{
    Task<EmpleadoResponse> CrearAsync(CrearEmpleadoRequest request, CancellationToken cancellationToken);
    Task<EmpleadoResponse> ObtenerAsync(int id, CancellationToken cancellationToken);
    Task<PaginatedResult<EmpleadoResponse>> BuscarAsync(BuscarEmpleadosRequest filtro, CancellationToken cancellationToken);
    Task<EmpleadoResponse> ActualizarAsync(int id, ActualizarEmpleadoRequest request, CancellationToken cancellationToken);
    Task CambiarEstadoAsync(int id, CambiarEstadoRequest request, CancellationToken cancellationToken);
    Task DesactivarAsync(int id, CancellationToken cancellationToken);
    Task<FotografiaResponse> ObtenerFotografiaAsync(int id, CancellationToken cancellationToken);
    Task ActualizarFotografiaAsync(int id, ActualizarFotografiaRequest request, CancellationToken cancellationToken);
    Task EliminarFotografiaAsync(int id, CancellationToken cancellationToken);
}
