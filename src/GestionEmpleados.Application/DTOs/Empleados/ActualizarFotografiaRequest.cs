using System.ComponentModel.DataAnnotations;

namespace GestionEmpleados.Application.DTOs.Empleados;

public sealed class ActualizarFotografiaRequest
{
    /// <summary>Imagen JPEG, PNG o WebP en Base64 puro o como Data URL.</summary>
    [Required]
    public required string FotografiaBase64 { get; init; }

    [MaxLength(50)]
    public string? FotografiaTipoContenido { get; init; }

    [MaxLength(255)]
    public string? FotografiaNombreArchivo { get; init; }
}
