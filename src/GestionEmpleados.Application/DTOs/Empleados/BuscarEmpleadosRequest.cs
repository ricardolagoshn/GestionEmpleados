using GestionEmpleados.Domain.Enums;

namespace GestionEmpleados.Application.DTOs.Empleados;

public sealed class BuscarEmpleadosRequest
{
    public const int PageSizeMaximo = 100;

    /// <summary>Busca en código, nombres, apellidos y cédula.</summary>
    public string? Search { get; init; }
    public int? PaisId { get; init; }
    public Genero? Genero { get; init; }
    public bool Activo { get; init; } = true;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    /// <summary>apellidos, nombres, fechaIngreso o codigoEmpleado.</summary>
    public string? SortBy { get; init; }

    /// <summary>asc o desc.</summary>
    public string? SortDirection { get; init; }
}
