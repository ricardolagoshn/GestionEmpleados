using GestionEmpleados.Application.Common.Exceptions;
using GestionEmpleados.Application.DTOs.Paises;
using GestionEmpleados.Application.Interfaces;
using GestionEmpleados.Domain.Entities;

namespace GestionEmpleados.Application.Services;

public sealed class PaisService(IPaisRepository paises) : IPaisService
{
    public async Task<IReadOnlyList<PaisResponse>> ListarActivosAsync(CancellationToken cancellationToken)
    {
        var lista = await paises.ListarActivosAsync(cancellationToken);
        return [.. lista.Select(Mapear)];
    }

    public async Task<PaisResponse> ObtenerAsync(int id, CancellationToken cancellationToken)
    {
        var pais = await paises.ObtenerPorIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"No existe un país con Id {id}.");

        return Mapear(pais);
    }

    private static PaisResponse Mapear(Pais p) => new(p.Id, p.CodigoIso2, p.CodigoIso3, p.Nombre, p.Activo);
}
