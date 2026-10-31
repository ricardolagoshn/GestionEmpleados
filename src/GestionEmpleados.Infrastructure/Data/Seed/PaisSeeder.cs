using GestionEmpleados.Domain.Entities;

namespace GestionEmpleados.Infrastructure.Data.Seed;

/// <summary>
/// Catálogo mínimo de países. Se inserta mediante migraciones (<c>HasData</c>), por lo que es idempotente.
/// </summary>
public static class PaisSeeder
{
    public static IReadOnlyList<Pais> Paises { get; } =
    [
        new() { Id = 1, CodigoIso2 = "HN", CodigoIso3 = "HND", Nombre = "Honduras" },
        new() { Id = 2, CodigoIso2 = "GT", CodigoIso3 = "GTM", Nombre = "Guatemala" },
        new() { Id = 3, CodigoIso2 = "SV", CodigoIso3 = "SLV", Nombre = "El Salvador" },
        new() { Id = 4, CodigoIso2 = "NI", CodigoIso3 = "NIC", Nombre = "Nicaragua" },
        new() { Id = 5, CodigoIso2 = "CR", CodigoIso3 = "CRI", Nombre = "Costa Rica" },
        new() { Id = 6, CodigoIso2 = "PA", CodigoIso3 = "PAN", Nombre = "Panamá" },
        new() { Id = 7, CodigoIso2 = "MX", CodigoIso3 = "MEX", Nombre = "México" },
        new() { Id = 8, CodigoIso2 = "US", CodigoIso3 = "USA", Nombre = "Estados Unidos" },
        new() { Id = 9, CodigoIso2 = "CO", CodigoIso3 = "COL", Nombre = "Colombia" },
        new() { Id = 10, CodigoIso2 = "ES", CodigoIso3 = "ESP", Nombre = "España" }
    ];
}
