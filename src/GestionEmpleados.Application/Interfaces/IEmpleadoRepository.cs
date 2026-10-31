using GestionEmpleados.Application.Common.Models;
using GestionEmpleados.Application.DTOs.Empleados;
using GestionEmpleados.Domain.Entities;

namespace GestionEmpleados.Application.Interfaces;

public interface IEmpleadoRepository
{
    /// <summary>Obtiene la entidad con seguimiento de cambios para modificarla.</summary>
    Task<Empleado?> ObtenerParaEdicionAsync(int id, CancellationToken cancellationToken);

    /// <summary>Proyección de solo lectura que no carga la fotografía.</summary>
    Task<EmpleadoResponse?> ObtenerResponseAsync(int id, CancellationToken cancellationToken);

    Task<PaginatedResult<EmpleadoResponse>> BuscarAsync(BuscarEmpleadosRequest filtro, CancellationToken cancellationToken);

    /// <summary>Devuelve <c>null</c> si el empleado no existe.</summary>
    Task<FotografiaEmpleado?> ObtenerFotografiaAsync(int id, CancellationToken cancellationToken);

    Task<bool> ExisteCodigoAsync(string codigoEmpleado, int? idExcluido, CancellationToken cancellationToken);
    Task<bool> ExisteCedulaAsync(int paisId, string cedula, int? idExcluido, CancellationToken cancellationToken);
    Task<bool> ExisteCorreoAsync(string correoElectronico, int? idExcluido, CancellationToken cancellationToken);

    void Agregar(Empleado empleado);

    /// <summary>Indica la versión que el cliente leyó, para el control de concurrencia optimista.</summary>
    void EstablecerVersionOriginal(Empleado empleado, byte[] version);

    /// <summary>Persiste los cambios; traduce duplicados y conflictos de concurrencia a <c>ConflictException</c>.</summary>
    Task GuardarCambiosAsync(CancellationToken cancellationToken);
}
