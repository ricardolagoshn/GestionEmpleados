using GestionEmpleados.Domain.Enums;
using GestionEmpleados.Domain.Exceptions;

namespace GestionEmpleados.Domain.Entities;

public sealed class Empleado
{
    public int Id { get; set; }
    public required string CodigoEmpleado { get; set; }
    public required string Nombres { get; set; }
    public required string Apellidos { get; set; }
    public DateOnly FechaNacimiento { get; set; }
    public required string Cedula { get; set; }

    public int PaisId { get; set; }
    public Pais Pais { get; set; } = null!;

    public required string Direccion { get; set; }
    public Genero Genero { get; set; }
    public string? Telefono { get; set; }
    public string? CorreoElectronico { get; set; }

    public DateOnly FechaIngreso { get; set; }
    public DateOnly? FechaSalida { get; set; }
    public string? Cargo { get; set; }

    public byte[]? Fotografia { get; set; }
    public string? FotografiaTipoContenido { get; set; }
    public string? FotografiaNombreArchivo { get; set; }

    public bool Activo { get; set; } = true;
    public DateTimeOffset FechaCreacionUtc { get; set; }
    public DateTimeOffset? FechaActualizacionUtc { get; set; }

    public byte[] Version { get; set; } = [];

    public void Desactivar(DateOnly? fechaSalida, DateTimeOffset ahoraUtc)
    {
        if (fechaSalida is { } salida && salida < FechaIngreso)
        {
            throw new DomainException("La fecha de salida no puede ser anterior a la fecha de ingreso.");
        }

        Activo = false;
        FechaSalida = fechaSalida ?? FechaSalida;
        FechaActualizacionUtc = ahoraUtc;
    }

    public void Activar(DateTimeOffset ahoraUtc)
    {
        Activo = true;
        FechaSalida = null;
        FechaActualizacionUtc = ahoraUtc;
    }

    public void AsignarFotografia(byte[] contenido, string tipoContenido, string? nombreArchivo, DateTimeOffset ahoraUtc)
    {
        ArgumentNullException.ThrowIfNull(contenido);
        ArgumentException.ThrowIfNullOrWhiteSpace(tipoContenido);

        Fotografia = contenido;
        FotografiaTipoContenido = tipoContenido;
        FotografiaNombreArchivo = nombreArchivo;
        FechaActualizacionUtc = ahoraUtc;
    }

    public void QuitarFotografia(DateTimeOffset ahoraUtc)
    {
        Fotografia = null;
        FotografiaTipoContenido = null;
        FotografiaNombreArchivo = null;
        FechaActualizacionUtc = ahoraUtc;
    }
}
