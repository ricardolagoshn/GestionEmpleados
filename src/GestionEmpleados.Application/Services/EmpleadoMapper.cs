using System.Linq.Expressions;
using GestionEmpleados.Application.DTOs.Empleados;
using GestionEmpleados.Domain.Entities;

namespace GestionEmpleados.Application.Services;

public static class EmpleadoMapper
{
    /// <summary>Proyección para consultas: no lee la columna <c>Fotografia</c>, solo comprueba si es nula.</summary>
    public static Expression<Func<Empleado, EmpleadoResponse>> Proyeccion { get; } = e => new EmpleadoResponse(
        e.Id,
        e.CodigoEmpleado,
        e.Nombres,
        e.Apellidos,
        e.FechaNacimiento,
        e.Cedula,
        e.PaisId,
        e.Pais.Nombre,
        e.Direccion,
        e.Genero.ToString(),
        e.Telefono,
        e.CorreoElectronico,
        e.FechaIngreso,
        e.FechaSalida,
        e.Cargo,
        e.Fotografia != null,
        e.Fotografia != null ? RutaFotografia(e.Id) : null,
        e.Activo,
        e.FechaCreacionUtc,
        e.FechaActualizacionUtc,
        Convert.ToBase64String(e.Version));

    public static EmpleadoResponse Mapear(Empleado e, string nombrePais)
    {
        ArgumentNullException.ThrowIfNull(e);

        return new EmpleadoResponse(
            e.Id,
            e.CodigoEmpleado,
            e.Nombres,
            e.Apellidos,
            e.FechaNacimiento,
            e.Cedula,
            e.PaisId,
            nombrePais,
            e.Direccion,
            e.Genero.ToString(),
            e.Telefono,
            e.CorreoElectronico,
            e.FechaIngreso,
            e.FechaSalida,
            e.Cargo,
            e.Fotografia is not null,
            e.Fotografia is not null ? RutaFotografia(e.Id) : null,
            e.Activo,
            e.FechaCreacionUtc,
            e.FechaActualizacionUtc,
            Convert.ToBase64String(e.Version));
    }

    public static string RutaFotografia(int id) => $"/api/empleados/{id}/fotografia";
}
