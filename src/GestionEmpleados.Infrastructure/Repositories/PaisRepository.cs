using GestionEmpleados.Application.Interfaces;
using GestionEmpleados.Domain.Entities;
using GestionEmpleados.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GestionEmpleados.Infrastructure.Repositories;

public sealed class PaisRepository(ApplicationDbContext context) : IPaisRepository
{
    public Task<Pais?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
        context.Paises.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Pais>> ListarActivosAsync(CancellationToken cancellationToken) =>
        await context.Paises
            .AsNoTracking()
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .ToListAsync(cancellationToken);
}
