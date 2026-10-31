namespace GestionEmpleados.Application.DTOs.Empleados;

/// <summary>Bytes de la fotografía listos para devolverse con su MIME.</summary>
public sealed record FotografiaResponse(byte[] Contenido, string TipoContenido, string? NombreArchivo);

/// <summary>Columnas de fotografía de un empleado existente; los valores son nulos si no tiene foto.</summary>
public sealed record FotografiaEmpleado(byte[]? Contenido, string? TipoContenido, string? NombreArchivo);
