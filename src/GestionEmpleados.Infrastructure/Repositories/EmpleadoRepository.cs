using System.Linq.Expressions;
using GestionEmpleados.Application.Common.Exceptions;
using GestionEmpleados.Application.Common.Models;
using GestionEmpleados.Application.DTOs.Empleados;
using GestionEmpleados.Application.Interfaces;
using GestionEmpleados.Application.Services;
using GestionEmpleados.Domain.Entities;
using GestionEmpleados.Infrastructure.Data;
using GestionEmpleados.Infrastructure.Data.Configurations;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GestionEmpleados.Infrastructure.Repositories;

public sealed class EmpleadoRepository(ApplicationDbContext context) : IEmpleadoRepository
{
    public Task<Empleado?> ObtenerParaEdicionAsync(int id, CancellationToken cancellationToken) =>
        context.Empleados.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<EmpleadoResponse?> ObtenerResponseAsync(int id, CancellationToken cancellationToken) =>
        context.Empleados
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(EmpleadoMapper.Proyeccion)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<PaginatedResult<EmpleadoResponse>> BuscarAsync(BuscarEmpleadosRequest filtro, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        var query = context.Empleados.AsNoTracking().Where(e => e.Activo == filtro.Activo);

        if (filtro.Search is { } search)
        {
            query = query.Where(e =>
                e.CodigoEmpleado.Contains(search)
                || e.Nombres.Contains(search)
                || e.Apellidos.Contains(search)
                || e.Cedula.Contains(search));
        }

        if (filtro.PaisId is { } paisId)
        {
            query = query.Where(e => e.PaisId == paisId);
        }

        if (filtro.Genero is { } genero)
        {
            query = query.Where(e => e.Genero == genero);
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await Ordenar(query, filtro.SortBy, filtro.SortDirection)
            .Skip((filtro.Page - 1) * filtro.PageSize)
            .Take(filtro.PageSize)
            .Select(EmpleadoMapper.Proyeccion)
            .ToListAsync(cancellationToken);

        return new PaginatedResult<EmpleadoResponse>(items, filtro.Page, filtro.PageSize, total);
    }

    public Task<FotografiaEmpleado?> ObtenerFotografiaAsync(int id, CancellationToken cancellationToken) =>
        context.Empleados
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new FotografiaEmpleado(e.Fotografia, e.FotografiaTipoContenido, e.FotografiaNombreArchivo))
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> ExisteCodigoAsync(string codigoEmpleado, int? idExcluido, CancellationToken cancellationToken) =>
        Otros(idExcluido).AnyAsync(e => e.CodigoEmpleado == codigoEmpleado, cancellationToken);

    public Task<bool> ExisteCedulaAsync(int paisId, string cedula, int? idExcluido, CancellationToken cancellationToken) =>
        Otros(idExcluido).AnyAsync(e => e.PaisId == paisId && e.Cedula == cedula, cancellationToken);

    public Task<bool> ExisteCorreoAsync(string correoElectronico, int? idExcluido, CancellationToken cancellationToken) =>
        Otros(idExcluido).AnyAsync(e => e.CorreoElectronico == correoElectronico, cancellationToken);

    public void Agregar(Empleado empleado) => context.Empleados.Add(empleado);

    public void EstablecerVersionOriginal(Empleado empleado, byte[] version) =>
        context.Entry(empleado).Property(e => e.Version).OriginalValue = version;

    public async Task GuardarCambiosAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(EmpleadoService.MensajeConcurrencia, exception);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 2601 or 2627 } sql)
        {
            // Respaldo ante condiciones de carrera: el servicio ya valida duplicados antes de guardar.
            throw new ConflictException(MensajeDuplicado(sql.Message), exception);
        }
    }

    private IQueryable<Empleado> Otros(int? idExcluido) =>
        idExcluido is { } id
            ? context.Empleados.AsNoTracking().Where(e => e.Id != id)
            : context.Empleados.AsNoTracking();

    private static IQueryable<Empleado> Ordenar(IQueryable<Empleado> query, string? sortBy, string? sortDirection)
    {
        var descendente = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        var ordenado = sortBy?.ToUpperInvariant() switch
        {
            "NOMBRES" => OrdenarPor(query, e => e.Nombres, descendente).ThenBy(e => e.Apellidos),
            "FECHAINGRESO" => OrdenarPor(query, e => e.FechaIngreso, descendente),
            "CODIGOEMPLEADO" => OrdenarPor(query, e => e.CodigoEmpleado, descendente),
            _ => OrdenarPor(query, e => e.Apellidos, descendente).ThenBy(e => e.Nombres)
        };

        // El Id garantiza un orden estable entre páginas.
        return ordenado.ThenBy(e => e.Id);
    }

    private static IOrderedQueryable<Empleado> OrdenarPor<TKey>(
        IQueryable<Empleado> query,
        Expression<Func<Empleado, TKey>> clave,
        bool descendente) =>
        descendente ? query.OrderByDescending(clave) : query.OrderBy(clave);

    private static string MensajeDuplicado(string mensajeSql)
    {
        if (mensajeSql.Contains(EmpleadoConfiguration.IndiceCodigo, StringComparison.OrdinalIgnoreCase))
        {
            return "Ya existe un empleado con el mismo código de empleado.";
        }

        if (mensajeSql.Contains(EmpleadoConfiguration.IndiceCedula, StringComparison.OrdinalIgnoreCase))
        {
            return "Ya existe un empleado con la misma cédula en el país indicado.";
        }

        if (mensajeSql.Contains(EmpleadoConfiguration.IndiceCorreo, StringComparison.OrdinalIgnoreCase))
        {
            return "Ya existe un empleado con el mismo correo electrónico.";
        }

        return "El registro entra en conflicto con datos existentes.";
    }
}
