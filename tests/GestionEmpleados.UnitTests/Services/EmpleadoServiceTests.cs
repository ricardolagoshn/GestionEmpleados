using GestionEmpleados.Application.Common.Exceptions;
using GestionEmpleados.Application.Common.Options;
using GestionEmpleados.Application.DTOs.Empleados;
using GestionEmpleados.Application.Services;
using GestionEmpleados.Application.Utilities;
using GestionEmpleados.Domain.Enums;
using GestionEmpleados.UnitTests.Fakes;

namespace GestionEmpleados.UnitTests.Services;

public sealed class EmpleadoServiceTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 10, 31, 12, 0, 0, TimeSpan.Zero);
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];

    private readonly FakeEmpleadoRepository empleados = new();
    private readonly FakePaisRepository paises = new();
    private readonly EmpleadoOptions opciones = new();

    private EmpleadoService CrearServicio() => new(empleados, paises, opciones, new FakeTimeProvider(Ahora));

    private static CrearEmpleadoRequest Solicitud(
        string codigo = "EMP-0001",
        string cedula = "0801-1995-12345",
        int paisId = 1,
        DateOnly? fechaNacimiento = null,
        DateOnly? fechaIngreso = null,
        DateOnly? fechaSalida = null,
        string nombres = "Ana María",
        string? correo = "ana.lopez@empresa.com",
        string? fotografiaBase64 = null,
        string? tipoContenido = null) => new()
        {
            CodigoEmpleado = codigo,
            Nombres = nombres,
            Apellidos = "López Rodríguez",
            FechaNacimiento = fechaNacimiento ?? new DateOnly(1995, 8, 20),
            Cedula = cedula,
            PaisId = paisId,
            Direccion = "Tegucigalpa, Francisco Morazán",
            Genero = Genero.Femenino,
            Telefono = "+504 9999-9999",
            CorreoElectronico = correo,
            FechaIngreso = fechaIngreso ?? new DateOnly(2026, 10, 31),
            FechaSalida = fechaSalida,
            Cargo = "Analista de sistemas",
            FotografiaBase64 = fotografiaBase64,
            FotografiaTipoContenido = tipoContenido,
            FotografiaNombreArchivo = fotografiaBase64 is null ? null : "C:\\fotos\\ana.jpg"
        };

    [Fact]
    public async Task CrearAsync_DatosValidos_CreaEmpleadoNormalizado()
    {
        var request = new CrearEmpleadoRequest
        {
            CodigoEmpleado = "  emp-0001 ",
            Nombres = "  Ana María ",
            Apellidos = " López Rodríguez ",
            FechaNacimiento = new DateOnly(1995, 8, 20),
            Cedula = " 0801-1995-12345 ",
            PaisId = 1,
            Direccion = " Tegucigalpa ",
            Genero = Genero.Femenino,
            CorreoElectronico = " Ana.Lopez@Empresa.com ",
            FechaIngreso = new DateOnly(2026, 10, 31),
            Telefono = "   "
        };

        var respuesta = await CrearServicio().CrearAsync(request, CancellationToken.None);

        var guardado = Assert.Single(empleados.Empleados);
        Assert.Equal("EMP-0001", guardado.CodigoEmpleado);
        Assert.Equal("Ana María", guardado.Nombres);
        Assert.Equal("López Rodríguez", guardado.Apellidos);
        Assert.Equal("0801-1995-12345", guardado.Cedula);
        Assert.Equal("ana.lopez@empresa.com", guardado.CorreoElectronico);
        Assert.Null(guardado.Telefono);
        Assert.Null(guardado.Fotografia);
        Assert.Null(guardado.FotografiaTipoContenido);
        Assert.Null(guardado.FotografiaNombreArchivo);
        Assert.True(guardado.Activo);
        Assert.Equal(Ahora, guardado.FechaCreacionUtc);
        Assert.Equal(1, empleados.Guardados);
        Assert.Equal("Honduras", respuesta.Pais);
        Assert.False(respuesta.TieneFotografia);
    }

    [Fact]
    public async Task CrearAsync_FechaNacimientoFutura_LanzaValidacion()
    {
        var ex = await Assert.ThrowsAsync<ValidacionException>(() =>
            CrearServicio().CrearAsync(Solicitud(fechaNacimiento: new DateOnly(2027, 1, 1)), CancellationToken.None));

        Assert.Contains("fechaNacimiento", ex.Errores.Keys);
    }

    [Fact]
    public async Task CrearAsync_EdadInferiorALaConfigurada_LanzaValidacion()
    {
        var ex = await Assert.ThrowsAsync<ValidacionException>(() =>
            CrearServicio().CrearAsync(Solicitud(fechaNacimiento: new DateOnly(2008, 11, 1)), CancellationToken.None));

        Assert.Contains("fechaNacimiento", ex.Errores.Keys);
    }

    [Fact]
    public async Task CrearAsync_EdadMinimaConfigurable_PermiteMenores()
    {
        opciones.EdadMinima = 16;

        await CrearServicio().CrearAsync(Solicitud(fechaNacimiento: new DateOnly(2008, 11, 1)), CancellationToken.None);

        Assert.Single(empleados.Empleados);
    }

    [Theory]
    [InlineData(99)]
    [InlineData(3)]
    public async Task CrearAsync_PaisInexistenteOInactivo_LanzaNotFound(int paisId)
    {
        await Assert.ThrowsAsync<NotFoundException>(() =>
            CrearServicio().CrearAsync(Solicitud(paisId: paisId), CancellationToken.None));

        Assert.Empty(empleados.Empleados);
    }

    [Fact]
    public async Task CrearAsync_CodigoDuplicadoSinDistinguirMayusculas_LanzaConflicto()
    {
        var servicio = CrearServicio();
        await servicio.CrearAsync(Solicitud(codigo: "EMP-0001"), CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() =>
            servicio.CrearAsync(Solicitud(codigo: "emp-0001", cedula: "99999", correo: null), CancellationToken.None));
    }

    [Fact]
    public async Task CrearAsync_CedulaDuplicadaEnMismoPais_LanzaConflicto()
    {
        var servicio = CrearServicio();
        await servicio.CrearAsync(Solicitud(), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<ConflictException>(() =>
            servicio.CrearAsync(Solicitud(codigo: "EMP-0002", correo: null), CancellationToken.None));

        Assert.Contains("cédula", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CrearAsync_MismaCedulaEnPaisesDistintos_Permite()
    {
        var servicio = CrearServicio();
        await servicio.CrearAsync(Solicitud(paisId: 1), CancellationToken.None);
        await servicio.CrearAsync(Solicitud(codigo: "EMP-0002", paisId: 2, correo: null), CancellationToken.None);

        Assert.Equal(2, empleados.Empleados.Count);
    }

    [Fact]
    public async Task CrearAsync_FechaSalidaAnteriorAIngreso_LanzaValidacion()
    {
        var ex = await Assert.ThrowsAsync<ValidacionException>(() => CrearServicio().CrearAsync(
            Solicitud(fechaIngreso: new DateOnly(2026, 10, 1), fechaSalida: new DateOnly(2026, 9, 30)),
            CancellationToken.None));

        Assert.Contains("fechaSalida", ex.Errores.Keys);
    }

    [Fact]
    public async Task CrearAsync_FotografiaDataUrlValida_GuardaBytesYMime()
    {
        var dataUrl = "data:image/png;base64," + Convert.ToBase64String(Png);

        var respuesta = await CrearServicio().CrearAsync(
            Solicitud(fotografiaBase64: dataUrl, tipoContenido: "image/png"),
            CancellationToken.None);

        var guardado = Assert.Single(empleados.Empleados);
        Assert.Equal(Png, guardado.Fotografia);
        Assert.Equal("image/png", guardado.FotografiaTipoContenido);
        Assert.Equal("ana.jpg", guardado.FotografiaNombreArchivo);
        Assert.True(respuesta.TieneFotografia);
        Assert.Equal("/api/empleados/1/fotografia", respuesta.FotografiaUrl);
    }

    [Fact]
    public async Task CrearAsync_Base64Invalido_LanzaValidacion()
    {
        var ex = await Assert.ThrowsAsync<ValidacionException>(() =>
            CrearServicio().CrearAsync(Solicitud(fotografiaBase64: "esto-no-es-base64!!"), CancellationToken.None));

        Assert.Contains("fotografiaBase64", ex.Errores.Keys);
    }

    [Fact]
    public async Task CrearAsync_ImagenMayorA5MB_LanzaArchivoDemasiadoGrande()
    {
        var bytes = new byte[Base64ImageConverter.MaxBytes + 1];
        Jpeg.CopyTo(bytes, 0);

        await Assert.ThrowsAsync<ArchivoDemasiadoGrandeException>(() =>
            CrearServicio().CrearAsync(Solicitud(fotografiaBase64: Convert.ToBase64String(bytes)), CancellationToken.None));
    }

    [Fact]
    public async Task CrearAsync_MimeNoPermitido_LanzaTipoNoPermitido()
    {
        await Assert.ThrowsAsync<TipoArchivoNoPermitidoException>(() => CrearServicio().CrearAsync(
            Solicitud(fotografiaBase64: Convert.ToBase64String(Jpeg), tipoContenido: "image/gif"),
            CancellationToken.None));
    }

    [Fact]
    public async Task CrearAsync_ContenidoSvg_LanzaTipoNoPermitido()
    {
        var svg = "<svg xmlns=\"http://www.w3.org/2000/svg\"></svg>"u8.ToArray();

        await Assert.ThrowsAsync<TipoArchivoNoPermitidoException>(() => CrearServicio().CrearAsync(
            Solicitud(fotografiaBase64: Convert.ToBase64String(svg)),
            CancellationToken.None));
    }

    [Fact]
    public async Task CrearAsync_FirmaNoCoincideConMime_LanzaValidacion()
    {
        var ex = await Assert.ThrowsAsync<ValidacionException>(() => CrearServicio().CrearAsync(
            Solicitud(fotografiaBase64: Convert.ToBase64String(Png), tipoContenido: "image/jpeg"),
            CancellationToken.None));

        Assert.Contains("fotografiaBase64", ex.Errores.Keys);
    }

    [Fact]
    public async Task CrearAsync_MimeDataUrlDistintoAlDeclarado_LanzaValidacion()
    {
        var dataUrl = "data:image/png;base64," + Convert.ToBase64String(Png);

        await Assert.ThrowsAsync<ValidacionException>(() => CrearServicio().CrearAsync(
            Solicitud(fotografiaBase64: dataUrl, tipoContenido: "image/webp"),
            CancellationToken.None));
    }

    [Fact]
    public async Task DesactivarAsync_EmpleadoExistente_DesactivaSinEliminar()
    {
        var servicio = CrearServicio();
        var creado = await servicio.CrearAsync(Solicitud(), CancellationToken.None);

        await servicio.DesactivarAsync(creado.Id, CancellationToken.None);

        var guardado = Assert.Single(empleados.Empleados);
        Assert.False(guardado.Activo);
        Assert.Equal(Ahora, guardado.FechaActualizacionUtc);
    }

    [Fact]
    public async Task DesactivarAsync_EmpleadoInexistente_LanzaNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => CrearServicio().DesactivarAsync(42, CancellationToken.None));
    }

    [Fact]
    public async Task ActualizarAsync_VersionAntigua_LanzaConflicto()
    {
        var servicio = CrearServicio();
        var creado = await servicio.CrearAsync(Solicitud(), CancellationToken.None);
        empleados.Empleados[0].Version = [9, 9, 9, 9, 9, 9, 9, 9];

        await Assert.ThrowsAsync<ConflictException>(() => servicio.ActualizarAsync(
            creado.Id,
            new ActualizarEmpleadoRequest
            {
                CodigoEmpleado = creado.CodigoEmpleado,
                Nombres = "Ana",
                Apellidos = creado.Apellidos,
                FechaNacimiento = creado.FechaNacimiento,
                Cedula = creado.Cedula,
                PaisId = creado.PaisId,
                Direccion = creado.Direccion,
                FechaIngreso = creado.FechaIngreso,
                Version = creado.Version
            },
            CancellationToken.None));
    }

    [Fact]
    public async Task BuscarAsync_CampoDeOrdenNoPermitido_LanzaValidacion()
    {
        var ex = await Assert.ThrowsAsync<ValidacionException>(() => CrearServicio().BuscarAsync(
            new BuscarEmpleadosRequest { SortBy = "cedula; DROP TABLE Empleados", PageSize = 500 },
            CancellationToken.None));

        Assert.Contains("sortBy", ex.Errores.Keys);
        Assert.Contains("pageSize", ex.Errores.Keys);
    }
}
