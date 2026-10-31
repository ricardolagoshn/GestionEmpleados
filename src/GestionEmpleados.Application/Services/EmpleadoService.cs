using System.ComponentModel.DataAnnotations;
using GestionEmpleados.Application.Common.Exceptions;
using GestionEmpleados.Application.Common.Models;
using GestionEmpleados.Application.Common.Options;
using GestionEmpleados.Application.DTOs.Empleados;
using GestionEmpleados.Application.Interfaces;
using GestionEmpleados.Application.Utilities;
using GestionEmpleados.Domain.Entities;
using GestionEmpleados.Domain.Enums;

namespace GestionEmpleados.Application.Services;

public sealed class EmpleadoService(
    IEmpleadoRepository empleados,
    IPaisRepository paises,
    EmpleadoOptions opciones,
    TimeProvider reloj) : IEmpleadoService
{
    public const string MensajeConcurrencia =
        "El empleado fue modificado por otro proceso. Recargue el registro e intente de nuevo.";

    private static readonly string[] CamposOrdenPermitidos = ["apellidos", "nombres", "fechaIngreso", "codigoEmpleado"];
    private static readonly EmailAddressAttribute ValidadorCorreo = new();

    public async Task<EmpleadoResponse> CrearAsync(CrearEmpleadoRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var datos = Normalizar(request);
        ValidarFormato(datos);

        var foto = string.IsNullOrWhiteSpace(request.FotografiaBase64)
            ? null
            : Base64ImageConverter.Decode(request.FotografiaBase64, request.FotografiaTipoContenido, opciones.FotografiaMaxBytes);

        var pais = await ValidarReferenciasAsync(datos, actual: null, cancellationToken);

        var empleado = new Empleado
        {
            CodigoEmpleado = datos.CodigoEmpleado,
            Nombres = datos.Nombres,
            Apellidos = datos.Apellidos,
            Cedula = datos.Cedula,
            Direccion = datos.Direccion,
            FechaCreacionUtc = Ahora()
        };
        Aplicar(empleado, datos);

        if (foto is not null)
        {
            empleado.Fotografia = foto.Contenido;
            empleado.FotografiaTipoContenido = foto.TipoContenido;
            empleado.FotografiaNombreArchivo = Base64ImageConverter.NormalizarNombreArchivo(request.FotografiaNombreArchivo);
        }

        empleados.Agregar(empleado);
        await empleados.GuardarCambiosAsync(cancellationToken);

        return EmpleadoMapper.Mapear(empleado, pais.Nombre);
    }

    public async Task<EmpleadoResponse> ObtenerAsync(int id, CancellationToken cancellationToken) =>
        await empleados.ObtenerResponseAsync(id, cancellationToken) ?? throw EmpleadoNoEncontrado(id);

    public Task<PaginatedResult<EmpleadoResponse>> BuscarAsync(BuscarEmpleadosRequest filtro, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        var errores = new Errores();

        if (filtro.Page < 1)
        {
            errores.Agregar("page", "La página debe ser mayor o igual a 1.");
        }

        if (filtro.PageSize is < 1 or > BuscarEmpleadosRequest.PageSizeMaximo)
        {
            errores.Agregar("pageSize", $"El tamaño de página debe estar entre 1 y {BuscarEmpleadosRequest.PageSizeMaximo}.");
        }

        var sortBy = Opcional(filtro.SortBy);
        if (sortBy is not null && !CamposOrdenPermitidos.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
        {
            errores.Agregar("sortBy", $"Solo se puede ordenar por: {string.Join(", ", CamposOrdenPermitidos)}.");
        }

        var sortDirection = Opcional(filtro.SortDirection);
        if (sortDirection is not null
            && !sortDirection.Equals("asc", StringComparison.OrdinalIgnoreCase)
            && !sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase))
        {
            errores.Agregar("sortDirection", "La dirección de orden debe ser 'asc' o 'desc'.");
        }

        if (filtro.Genero is { } genero && !Enum.IsDefined(genero))
        {
            errores.Agregar("genero", "El género indicado no es válido.");
        }

        errores.LanzarSiExisten();

        var normalizado = new BuscarEmpleadosRequest
        {
            Search = Opcional(filtro.Search),
            PaisId = filtro.PaisId,
            Genero = filtro.Genero,
            Activo = filtro.Activo,
            Page = filtro.Page,
            PageSize = filtro.PageSize,
            SortBy = sortBy,
            SortDirection = sortDirection
        };

        return empleados.BuscarAsync(normalizado, cancellationToken);
    }

    public async Task<EmpleadoResponse> ActualizarAsync(int id, ActualizarEmpleadoRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var version = DecodificarVersion(request.Version);
        var datos = Normalizar(request);
        ValidarFormato(datos);

        var empleado = await empleados.ObtenerParaEdicionAsync(id, cancellationToken) ?? throw EmpleadoNoEncontrado(id);

        if (!empleado.Version.AsSpan().SequenceEqual(version))
        {
            throw new ConflictException(MensajeConcurrencia);
        }

        var pais = await ValidarReferenciasAsync(datos, empleado, cancellationToken);

        empleados.EstablecerVersionOriginal(empleado, version);
        Aplicar(empleado, datos);
        empleado.FechaActualizacionUtc = Ahora();

        await empleados.GuardarCambiosAsync(cancellationToken);

        return EmpleadoMapper.Mapear(empleado, pais.Nombre);
    }

    public async Task CambiarEstadoAsync(int id, CambiarEstadoRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var empleado = await empleados.ObtenerParaEdicionAsync(id, cancellationToken) ?? throw EmpleadoNoEncontrado(id);

        if (request.Activo)
        {
            empleado.Activar(Ahora());
        }
        else
        {
            empleado.Desactivar(request.FechaSalida, Ahora());
        }

        await empleados.GuardarCambiosAsync(cancellationToken);
    }

    public async Task DesactivarAsync(int id, CancellationToken cancellationToken)
    {
        var empleado = await empleados.ObtenerParaEdicionAsync(id, cancellationToken) ?? throw EmpleadoNoEncontrado(id);

        empleado.Desactivar(fechaSalida: null, Ahora());
        await empleados.GuardarCambiosAsync(cancellationToken);
    }

    public async Task<FotografiaResponse> ObtenerFotografiaAsync(int id, CancellationToken cancellationToken)
    {
        var foto = await empleados.ObtenerFotografiaAsync(id, cancellationToken) ?? throw EmpleadoNoEncontrado(id);

        if (foto.Contenido is null || foto.TipoContenido is null)
        {
            throw new NotFoundException($"El empleado con Id {id} no tiene fotografía.");
        }

        return new FotografiaResponse(foto.Contenido, foto.TipoContenido, foto.NombreArchivo);
    }

    public async Task ActualizarFotografiaAsync(int id, ActualizarFotografiaRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var empleado = await empleados.ObtenerParaEdicionAsync(id, cancellationToken) ?? throw EmpleadoNoEncontrado(id);
        var foto = Base64ImageConverter.Decode(request.FotografiaBase64, request.FotografiaTipoContenido, opciones.FotografiaMaxBytes);

        empleado.AsignarFotografia(
            foto.Contenido,
            foto.TipoContenido,
            Base64ImageConverter.NormalizarNombreArchivo(request.FotografiaNombreArchivo),
            Ahora());

        await empleados.GuardarCambiosAsync(cancellationToken);
    }

    public async Task EliminarFotografiaAsync(int id, CancellationToken cancellationToken)
    {
        var empleado = await empleados.ObtenerParaEdicionAsync(id, cancellationToken) ?? throw EmpleadoNoEncontrado(id);

        empleado.QuitarFotografia(Ahora());
        await empleados.GuardarCambiosAsync(cancellationToken);
    }

    private async Task<Pais> ValidarReferenciasAsync(DatosEmpleado datos, Empleado? actual, CancellationToken cancellationToken)
    {
        var pais = await paises.ObtenerPorIdAsync(datos.PaisId, cancellationToken);

        // Un empleado existente puede conservar un país que luego fue desactivado.
        if (pais is null || (!pais.Activo && pais.Id != actual?.PaisId))
        {
            throw new NotFoundException($"No existe un país activo con Id {datos.PaisId}.");
        }

        var idExcluido = actual?.Id;

        if (await empleados.ExisteCodigoAsync(datos.CodigoEmpleado, idExcluido, cancellationToken))
        {
            throw new ConflictException("Ya existe un empleado con el mismo código de empleado.");
        }

        if (await empleados.ExisteCedulaAsync(datos.PaisId, datos.Cedula, idExcluido, cancellationToken))
        {
            throw new ConflictException("Ya existe un empleado con la misma cédula en el país indicado.");
        }

        if (datos.CorreoElectronico is not null
            && await empleados.ExisteCorreoAsync(datos.CorreoElectronico, idExcluido, cancellationToken))
        {
            throw new ConflictException("Ya existe un empleado con el mismo correo electrónico.");
        }

        return pais;
    }

    private void ValidarFormato(DatosEmpleado d)
    {
        var errores = new Errores();
        var hoy = DateOnly.FromDateTime(reloj.GetUtcNow().UtcDateTime);

        errores.ValidarLongitud("codigoEmpleado", "El código de empleado", d.CodigoEmpleado, 3, 20);
        errores.ValidarLongitud("nombres", "Los nombres", d.Nombres, 2, 100);
        errores.ValidarLongitud("apellidos", "Los apellidos", d.Apellidos, 2, 100);
        errores.ValidarLongitud("cedula", "La cédula", d.Cedula, 5, 30);
        errores.ValidarLongitud("direccion", "La dirección", d.Direccion, 1, 300);

        if (d.FechaNacimiento == default)
        {
            errores.Agregar("fechaNacimiento", "La fecha de nacimiento es obligatoria.");
        }
        else if (d.FechaNacimiento > hoy)
        {
            errores.Agregar("fechaNacimiento", "La fecha de nacimiento no puede ser futura.");
        }
        else if (CalcularEdad(d.FechaNacimiento, hoy) < opciones.EdadMinima)
        {
            errores.Agregar("fechaNacimiento", $"El empleado debe tener al menos {opciones.EdadMinima} años.");
        }

        if (d.PaisId <= 0)
        {
            errores.Agregar("paisId", "Debe indicar un país válido.");
        }

        if (!Enum.IsDefined(d.Genero))
        {
            errores.Agregar("genero", "El género indicado no es válido.");
        }

        if (d.Telefono is { Length: > 25 })
        {
            errores.Agregar("telefono", "El teléfono no puede superar 25 caracteres.");
        }

        if (d.CorreoElectronico is not null)
        {
            if (d.CorreoElectronico.Length > 150)
            {
                errores.Agregar("correoElectronico", "El correo electrónico no puede superar 150 caracteres.");
            }
            else if (!ValidadorCorreo.IsValid(d.CorreoElectronico))
            {
                errores.Agregar("correoElectronico", "El correo electrónico no tiene un formato válido.");
            }
        }

        if (d.FechaIngreso == default)
        {
            errores.Agregar("fechaIngreso", "La fecha de ingreso es obligatoria.");
        }
        else if (d.FechaSalida is { } salida && salida < d.FechaIngreso)
        {
            errores.Agregar("fechaSalida", "La fecha de salida no puede ser anterior a la fecha de ingreso.");
        }

        if (d.Cargo is { Length: > 100 })
        {
            errores.Agregar("cargo", "El cargo no puede superar 100 caracteres.");
        }

        errores.LanzarSiExisten();
    }

    private static void Aplicar(Empleado empleado, DatosEmpleado d)
    {
        empleado.CodigoEmpleado = d.CodigoEmpleado;
        empleado.Nombres = d.Nombres;
        empleado.Apellidos = d.Apellidos;
        empleado.FechaNacimiento = d.FechaNacimiento;
        empleado.Cedula = d.Cedula;
        empleado.PaisId = d.PaisId;
        empleado.Direccion = d.Direccion;
        empleado.Genero = d.Genero;
        empleado.Telefono = d.Telefono;
        empleado.CorreoElectronico = d.CorreoElectronico;
        empleado.FechaIngreso = d.FechaIngreso;
        empleado.FechaSalida = d.FechaSalida;
        empleado.Cargo = d.Cargo;
    }

    private static DatosEmpleado Normalizar(EmpleadoRequestBase r) => new(
        CodigoEmpleado: (r.CodigoEmpleado ?? string.Empty).Trim().ToUpperInvariant(),
        Nombres: (r.Nombres ?? string.Empty).Trim(),
        Apellidos: (r.Apellidos ?? string.Empty).Trim(),
        FechaNacimiento: r.FechaNacimiento,
        Cedula: (r.Cedula ?? string.Empty).Trim(),
        PaisId: r.PaisId,
        Direccion: (r.Direccion ?? string.Empty).Trim(),
        Genero: r.Genero,
        Telefono: Opcional(r.Telefono),
#pragma warning disable CA1308 // El correo se normaliza a minúsculas por especificación.
        CorreoElectronico: Opcional(r.CorreoElectronico)?.ToLowerInvariant(),
#pragma warning restore CA1308
        FechaIngreso: r.FechaIngreso,
        FechaSalida: r.FechaSalida,
        Cargo: Opcional(r.Cargo));

    private static byte[] DecodificarVersion(string? version)
    {
        if (!string.IsNullOrWhiteSpace(version))
        {
            try
            {
                var bytes = Convert.FromBase64String(version.Trim());
                if (bytes.Length > 0)
                {
                    return bytes;
                }
            }
            catch (FormatException)
            {
                // Se reporta como error de validación abajo.
            }
        }

        throw new ValidacionException("version", "La versión del registro es obligatoria y debe estar en Base64.");
    }

    private static int CalcularEdad(DateOnly nacimiento, DateOnly hoy)
    {
        var edad = hoy.Year - nacimiento.Year;
        return nacimiento > hoy.AddYears(-edad) ? edad - 1 : edad;
    }

    private static string? Opcional(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static NotFoundException EmpleadoNoEncontrado(int id) => new($"No existe un empleado con Id {id}.");

    /// <summary>Hora del servidor truncada a segundos, igual a la precisión de <c>DATETIMEOFFSET(0)</c>.</summary>
    private DateTimeOffset Ahora()
    {
        var ahora = reloj.GetUtcNow();
        return new DateTimeOffset(ahora.Ticks - ahora.Ticks % TimeSpan.TicksPerSecond, TimeSpan.Zero);
    }

    private sealed record DatosEmpleado(
        string CodigoEmpleado,
        string Nombres,
        string Apellidos,
        DateOnly FechaNacimiento,
        string Cedula,
        int PaisId,
        string Direccion,
        Genero Genero,
        string? Telefono,
        string? CorreoElectronico,
        DateOnly FechaIngreso,
        DateOnly? FechaSalida,
        string? Cargo);

    private sealed class Errores
    {
        private readonly Dictionary<string, List<string>> errores = [];

        public void Agregar(string campo, string mensaje)
        {
            if (!errores.TryGetValue(campo, out var lista))
            {
                lista = [];
                errores[campo] = lista;
            }

            lista.Add(mensaje);
        }

        public void ValidarLongitud(string campo, string etiqueta, string valor, int minimo, int maximo)
        {
            if (valor.Length == 0)
            {
                Agregar(campo, $"{etiqueta} es obligatorio.");
            }
            else if (valor.Length < minimo || valor.Length > maximo)
            {
                Agregar(campo, $"{etiqueta} debe tener entre {minimo} y {maximo} caracteres.");
            }
        }

        public void LanzarSiExisten()
        {
            if (errores.Count > 0)
            {
                throw new ValidacionException(errores.ToDictionary(e => e.Key, e => e.Value.ToArray()));
            }
        }
    }
}
