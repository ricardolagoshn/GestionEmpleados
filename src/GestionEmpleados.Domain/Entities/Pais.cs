namespace GestionEmpleados.Domain.Entities;

public sealed class Pais
{
    public int Id { get; set; }
    public required string CodigoIso2 { get; set; }
    public required string CodigoIso3 { get; set; }
    public required string Nombre { get; set; }
    public bool Activo { get; set; } = true;

    public ICollection<Empleado> Empleados { get; init; } = [];
}
