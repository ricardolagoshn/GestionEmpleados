using System.ComponentModel.DataAnnotations;
using GestionEmpleados.Domain.Enums;

namespace GestionEmpleados.Application.DTOs.Empleados;

/// <summary>Datos editables comunes a la creación y actualización de un empleado.</summary>
public abstract class EmpleadoRequestBase
{
    [Required, MinLength(3), MaxLength(20)]
    public required string CodigoEmpleado { get; init; }

    [Required, MinLength(2), MaxLength(100)]
    public required string Nombres { get; init; }

    [Required, MinLength(2), MaxLength(100)]
    public required string Apellidos { get; init; }

    [Required]
    public DateOnly FechaNacimiento { get; init; }

    [Required, MinLength(5), MaxLength(30)]
    public required string Cedula { get; init; }

    [Range(1, int.MaxValue)]
    public int PaisId { get; init; }

    [Required, MaxLength(300)]
    public required string Direccion { get; init; }

    public Genero Genero { get; init; }

    [MaxLength(25)]
    public string? Telefono { get; init; }

    [EmailAddress, MaxLength(150)]
    public string? CorreoElectronico { get; init; }

    [Required]
    public DateOnly FechaIngreso { get; init; }

    public DateOnly? FechaSalida { get; init; }

    [MaxLength(100)]
    public string? Cargo { get; init; }
}
