using System.ComponentModel.DataAnnotations;

namespace GestionEmpleados.Application.DTOs.Empleados;

/// <summary>
/// Reemplaza todos los datos editables. La fotografía se administra en su propio endpoint.
/// </summary>
public sealed class ActualizarEmpleadoRequest : EmpleadoRequestBase
{
    /// <summary>Valor <c>version</c> recibido en la última consulta, codificado en Base64.</summary>
    [Required]
    public required string Version { get; init; }
}
