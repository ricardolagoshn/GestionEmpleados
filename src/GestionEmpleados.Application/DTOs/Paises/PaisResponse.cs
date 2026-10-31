namespace GestionEmpleados.Application.DTOs.Paises;

public sealed record PaisResponse(int Id, string CodigoIso2, string CodigoIso3, string Nombre, bool Activo);
