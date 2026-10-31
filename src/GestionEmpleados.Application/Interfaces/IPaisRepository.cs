using GestionEmpleados.Domain.Entities;

namespace GestionEmpleados.Application.Interfaces;

public interface IPaisRepository
{
    Task<Pais?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Pais>> ListarActivosAsync(CancellationToken cancellationToken);
}
