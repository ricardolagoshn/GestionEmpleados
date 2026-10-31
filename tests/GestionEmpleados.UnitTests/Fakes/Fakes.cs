using GestionEmpleados.Application.Common.Models;
using GestionEmpleados.Application.DTOs.Empleados;
using GestionEmpleados.Application.Interfaces;
using GestionEmpleados.Application.Services;
using GestionEmpleados.Domain.Entities;

namespace GestionEmpleados.UnitTests.Fakes;

internal sealed class FakeTimeProvider(DateTimeOffset ahora) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => ahora;
}

internal sealed class FakePaisRepository : IPaisRepository
{
    public List<Pais> Paises { get; } =
    [
        new() { Id = 1, CodigoIso2 = "HN", CodigoIso3 = "HND", Nombre = "Honduras" },
        new() { Id = 2, CodigoIso2 = "GT", CodigoIso3 = "GTM", Nombre = "Guatemala" },
        new() { Id = 3, CodigoIso2 = "XX", CodigoIso3 = "XXX", Nombre = "Inactivo", Activo = false }
    ];

    public Task<Pais?> ObtenerPorIdAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(Paises.FirstOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<Pais>> ListarActivosAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Pais>>([.. Paises.Where(p => p.Activo)]);
}

internal sealed class FakeEmpleadoRepository : IEmpleadoRepository
{
    private int siguienteId = 1;

    public List<Empleado> Empleados { get; } = [];
    public int Guardados { get; private set; }

    public Task<Empleado?> ObtenerParaEdicionAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(Empleados.FirstOrDefault(e => e.Id == id));

    public Task<EmpleadoResponse?> ObtenerResponseAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(Empleados.Where(e => e.Id == id).Select(e => EmpleadoMapper.Mapear(e, "País")).FirstOrDefault());

    public Task<PaginatedResult<EmpleadoResponse>> BuscarAsync(BuscarEmpleadosRequest filtro, CancellationToken cancellationToken)
    {
        var items = Empleados.Where(e => e.Activo == filtro.Activo).Select(e => EmpleadoMapper.Mapear(e, "País")).ToList();
        return Task.FromResult(new PaginatedResult<EmpleadoResponse>(items, filtro.Page, filtro.PageSize, items.Count));
    }

    public Task<FotografiaEmpleado?> ObtenerFotografiaAsync(int id, CancellationToken cancellationToken) =>
        Task.FromResult(Empleados
            .Where(e => e.Id == id)
            .Select(e => new FotografiaEmpleado(e.Fotografia, e.FotografiaTipoContenido, e.FotografiaNombreArchivo))
            .FirstOrDefault());

    public Task<bool> ExisteCodigoAsync(string codigoEmpleado, int? idExcluido, CancellationToken cancellationToken) =>
        Task.FromResult(Otros(idExcluido).Any(e => string.Equals(e.CodigoEmpleado, codigoEmpleado, StringComparison.OrdinalIgnoreCase)));

    public Task<bool> ExisteCedulaAsync(int paisId, string cedula, int? idExcluido, CancellationToken cancellationToken) =>
        Task.FromResult(Otros(idExcluido).Any(e => e.PaisId == paisId && e.Cedula == cedula));

    public Task<bool> ExisteCorreoAsync(string correoElectronico, int? idExcluido, CancellationToken cancellationToken) =>
        Task.FromResult(Otros(idExcluido).Any(e => e.CorreoElectronico == correoElectronico));

    public void Agregar(Empleado empleado)
    {
        empleado.Id = siguienteId++;
        empleado.Version = BitConverter.GetBytes((long)empleado.Id);
        Empleados.Add(empleado);
    }

    public void EstablecerVersionOriginal(Empleado empleado, byte[] version)
    {
    }

    public Task GuardarCambiosAsync(CancellationToken cancellationToken)
    {
        Guardados++;
        return Task.CompletedTask;
    }

    private IEnumerable<Empleado> Otros(int? idExcluido) => Empleados.Where(e => e.Id != idExcluido);
}
