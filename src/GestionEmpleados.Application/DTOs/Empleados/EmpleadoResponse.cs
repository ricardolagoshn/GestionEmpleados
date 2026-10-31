namespace GestionEmpleados.Application.DTOs.Empleados;

public sealed record EmpleadoResponse(
    int Id,
    string CodigoEmpleado,
    string Nombres,
    string Apellidos,
    DateOnly FechaNacimiento,
    string Cedula,
    int PaisId,
    string Pais,
    string Direccion,
    string Genero,
    string? Telefono,
    string? CorreoElectronico,
    DateOnly FechaIngreso,
    DateOnly? FechaSalida,
    string? Cargo,
    bool TieneFotografia,
    string? FotografiaUrl,
    bool Activo,
    DateTimeOffset FechaCreacionUtc,
    DateTimeOffset? FechaActualizacionUtc,
    string Version);
