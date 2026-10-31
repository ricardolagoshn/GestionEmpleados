namespace GestionEmpleados.Application.DTOs.Empleados;

public sealed class CambiarEstadoRequest
{
    public required bool Activo { get; init; }

    /// <summary>Opcional al desactivar. Al activar se limpia la fecha de salida.</summary>
    public DateOnly? FechaSalida { get; init; }
}
