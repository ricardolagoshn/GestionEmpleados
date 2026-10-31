using System.ComponentModel.DataAnnotations;

namespace GestionEmpleados.Application.DTOs.Empleados;

public sealed class CrearEmpleadoRequest : EmpleadoRequestBase
{
    /// <summary>Imagen JPEG, PNG o WebP en Base64 puro o como Data URL.</summary>
    public string? FotografiaBase64 { get; init; }

    [MaxLength(50)]
    public string? FotografiaTipoContenido { get; init; }

    [MaxLength(255)]
    public string? FotografiaNombreArchivo { get; init; }
}
