using GestionEmpleados.Application.DTOs.Paises;

namespace GestionEmpleados.Application.Interfaces;

public interface IPaisService
{
    Task<IReadOnlyList<PaisResponse>> ListarActivosAsync(CancellationToken cancellationToken);
    Task<PaisResponse> ObtenerAsync(int id, CancellationToken cancellationToken);
}
