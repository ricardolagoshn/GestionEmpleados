using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GestionEmpleados.Application.Common.Models;
using GestionEmpleados.Application.DTOs.Empleados;
using GestionEmpleados.IntegrationTests.Infrastructure;

namespace GestionEmpleados.IntegrationTests.Endpoints;

[Collection(ApiTestGroup.Nombre)]
public sealed class EmpleadosEndpointsTests(ApiFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

    private readonly HttpClient client = factory.CreateClient();

    private static Dictionary<string, object?> NuevoEmpleado(
        string? token = null,
        int paisId = 1,
        string genero = "femenino",
        string? fotografiaBase64 = null)
    {
        token ??= Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();

        return new Dictionary<string, object?>
        {
            ["codigoEmpleado"] = $"IT-{token}",
            ["nombres"] = "Ana María",
            ["apellidos"] = $"López {token}",
            ["fechaNacimiento"] = "1995-08-20",
            ["cedula"] = $"CED-{token}",
            ["paisId"] = paisId,
            ["direccion"] = "Tegucigalpa, Francisco Morazán",
            ["genero"] = genero,
            ["telefono"] = "+504 9999-9999",
            ["correoElectronico"] = $"{token.ToUpperInvariant()}@Prueba.Local",
            ["fechaIngreso"] = "2026-10-31",
            ["cargo"] = "Analista de sistemas",
            ["fotografiaBase64"] = fotografiaBase64,
            ["fotografiaTipoContenido"] = fotografiaBase64 is null ? null : "image/png",
            ["fotografiaNombreArchivo"] = fotografiaBase64 is null ? null : "ana.png"
        };
    }

    private async Task<EmpleadoResponse> CrearAsync(Dictionary<string, object?> body)
    {
        using var response = await client.PostAsJsonAsync("/api/empleados", body, Json);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EmpleadoResponse>(Json))!;
    }

    private static Dictionary<string, object?> Actualizacion(EmpleadoResponse e, string version, string cargo) => new()
    {
        ["codigoEmpleado"] = e.CodigoEmpleado,
        ["nombres"] = e.Nombres,
        ["apellidos"] = e.Apellidos,
        ["fechaNacimiento"] = e.FechaNacimiento,
        ["cedula"] = e.Cedula,
        ["paisId"] = e.PaisId,
        ["direccion"] = e.Direccion,
        ["genero"] = e.Genero,
        ["correoElectronico"] = e.CorreoElectronico,
        ["fechaIngreso"] = e.FechaIngreso,
        ["cargo"] = cargo,
        ["version"] = version
    };

    [Fact]
    public async Task Post_Valido_Retorna201ConLocation()
    {
        using var response = await client.PostAsJsonAsync("/api/empleados", NuevoEmpleado(), Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var creado = await response.Content.ReadFromJsonAsync<EmpleadoResponse>(Json);
        Assert.NotNull(creado);
        Assert.Equal($"/api/empleados/{creado.Id}", response.Headers.Location?.AbsolutePath);
        Assert.Equal("Honduras", creado.Pais);
        Assert.Equal("Femenino", creado.Genero);
        Assert.Equal(creado.CorreoElectronico, creado.CorreoElectronico?.ToLowerInvariant());
        Assert.False(string.IsNullOrEmpty(creado.Version));
    }

    [Fact]
    public async Task GetPorId_Existente_RetornaEmpleado()
    {
        var creado = await CrearAsync(NuevoEmpleado());

        var obtenido = await client.GetFromJsonAsync<EmpleadoResponse>($"/api/empleados/{creado.Id}", Json);

        Assert.Equal(creado, obtenido);
    }

    [Fact]
    public async Task GetListado_ConFotografia_NoIncluyeBytesNiBase64()
    {
        var token = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        var base64 = Convert.ToBase64String(Png);
        await CrearAsync(NuevoEmpleado(token, fotografiaBase64: base64));

        var json = await client.GetStringAsync($"/api/empleados?search={token}");

        Assert.DoesNotContain(base64, json, StringComparison.Ordinal);
        Assert.DoesNotContain("fotografiaBase64", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"tieneFotografia\":true", json, StringComparison.Ordinal);
        Assert.Contains("\"totalCount\":1", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetListado_Filtros_RetornanSoloCoincidencias()
    {
        var token = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        await CrearAsync(NuevoEmpleado(token + "A1", paisId: 1, genero: "femenino"));
        await CrearAsync(NuevoEmpleado(token + "B2", paisId: 2, genero: "masculino"));
        await CrearAsync(NuevoEmpleado(token + "C3", paisId: 2, genero: "femenino"));

        var porPais = await client.GetFromJsonAsync<PaginatedResult<EmpleadoResponse>>(
            $"/api/empleados?search={token}&paisId=2&sortBy=codigoEmpleado&sortDirection=desc", Json);
        var porGenero = await client.GetFromJsonAsync<PaginatedResult<EmpleadoResponse>>(
            $"/api/empleados?search={token}&genero=femenino&pageSize=1&page=2&sortBy=codigoEmpleado", Json);

        Assert.Equal(2, porPais!.TotalCount);
        Assert.Equal([$"IT-{token}C3", $"IT-{token}B2"], porPais.Items.Select(e => e.CodigoEmpleado));
        Assert.Equal(2, porGenero!.TotalCount);
        Assert.Equal($"IT-{token}C3", Assert.Single(porGenero.Items).CodigoEmpleado);
    }

    [Fact]
    public async Task GetListado_OrdenNoPermitido_Retorna400()
    {
        using var response = await client.GetAsync("/api/empleados?sortBy=cedula");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetFotografia_RetornaBytesConMimeCorrecto()
    {
        var creado = await CrearAsync(NuevoEmpleado(fotografiaBase64: "data:image/png;base64," + Convert.ToBase64String(Png)));

        using var response = await client.GetAsync(creado.FotografiaUrl);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("image/png", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Png, await response.Content.ReadAsByteArrayAsync());
    }

    [Fact]
    public async Task PutYDeleteFotografia_ReemplazaYQuita()
    {
        var creado = await CrearAsync(NuevoEmpleado());
        var jpeg = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 };

        using var put = await client.PutAsJsonAsync(
            $"/api/empleados/{creado.Id}/fotografia",
            new { fotografiaBase64 = Convert.ToBase64String(jpeg), fotografiaTipoContenido = "image/jpeg" },
            Json);
        using var get = await client.GetAsync($"/api/empleados/{creado.Id}/fotografia");
        using var delete = await client.DeleteAsync($"/api/empleados/{creado.Id}/fotografia");
        using var getDespues = await client.GetAsync($"/api/empleados/{creado.Id}/fotografia");

        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);
        Assert.Equal("image/jpeg", get.Content.Headers.ContentType?.MediaType);
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getDespues.StatusCode);
    }

    [Fact]
    public async Task PutFotografia_TipoNoPermitido_Retorna415()
    {
        var creado = await CrearAsync(NuevoEmpleado());
        var gif = "GIF89a\0\0"u8.ToArray();

        using var response = await client.PutAsJsonAsync(
            $"/api/empleados/{creado.Id}/fotografia",
            new { fotografiaBase64 = Convert.ToBase64String(gif) },
            Json);

        Assert.Equal(HttpStatusCode.UnsupportedMediaType, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Post_CodigoDuplicado_Retorna409()
    {
        var token = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        await CrearAsync(NuevoEmpleado(token));

        var duplicado = NuevoEmpleado();
        duplicado["codigoEmpleado"] = $"it-{token.ToLowerInvariant()}";
        using var response = await client.PostAsJsonAsync("/api/empleados", duplicado, Json);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        Assert.Equal(409, problem.GetProperty("status").GetInt32());
        Assert.Equal("/api/empleados", problem.GetProperty("instance").GetString());
        Assert.True(problem.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task Post_CedulaDuplicadaEnMismoPais_Retorna409()
    {
        var token = Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        await CrearAsync(NuevoEmpleado(token));

        var duplicado = NuevoEmpleado();
        duplicado["cedula"] = $"CED-{token}";
        using var response = await client.PostAsJsonAsync("/api/empleados", duplicado, Json);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Post_DatosInvalidos_Retorna400ConProblemDetails()
    {
        var body = NuevoEmpleado();
        body["fechaNacimiento"] = "2030-01-01";
        body["fechaSalida"] = "2020-01-01";

        using var response = await client.PostAsJsonAsync("/api/empleados", body, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        var errores = problem.GetProperty("errors");
        Assert.True(errores.TryGetProperty("fechaNacimiento", out _));
        Assert.True(errores.TryGetProperty("fechaSalida", out _));
    }

    [Fact]
    public async Task Post_AnotacionesInvalidas_Retorna400ConProblemDetails()
    {
        var body = NuevoEmpleado();
        body["nombres"] = "A";

        using var response = await client.PostAsJsonAsync("/api/empleados", body, Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Get_EmpleadoInexistente_Retorna404()
    {
        using var response = await client.GetAsync("/api/empleados/999999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Put_VersionAntigua_Retorna409()
    {
        var creado = await CrearAsync(NuevoEmpleado());

        using var primera = await client.PutAsJsonAsync(
            $"/api/empleados/{creado.Id}", Actualizacion(creado, creado.Version, "Líder técnico"), Json);
        using var segunda = await client.PutAsJsonAsync(
            $"/api/empleados/{creado.Id}", Actualizacion(creado, creado.Version, "Gerente"), Json);

        Assert.Equal(HttpStatusCode.OK, primera.StatusCode);
        var actualizado = await primera.Content.ReadFromJsonAsync<EmpleadoResponse>(Json);
        Assert.Equal("Líder técnico", actualizado!.Cargo);
        Assert.NotEqual(creado.Version, actualizado.Version);
        Assert.NotNull(actualizado.FechaActualizacionUtc);
        Assert.Equal(HttpStatusCode.Conflict, segunda.StatusCode);
    }

    [Fact]
    public async Task Delete_DesactivaSinEliminarLaFila()
    {
        var creado = await CrearAsync(NuevoEmpleado());

        using var delete = await client.DeleteAsync($"/api/empleados/{creado.Id}");
        var obtenido = await client.GetFromJsonAsync<EmpleadoResponse>($"/api/empleados/{creado.Id}", Json);
        var activos = await client.GetFromJsonAsync<PaginatedResult<EmpleadoResponse>>(
            $"/api/empleados?search={creado.CodigoEmpleado}", Json);
        var inactivos = await client.GetFromJsonAsync<PaginatedResult<EmpleadoResponse>>(
            $"/api/empleados?search={creado.CodigoEmpleado}&activo=false", Json);

        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.False(obtenido!.Activo);
        Assert.Equal(0, activos!.TotalCount);
        Assert.Equal(1, inactivos!.TotalCount);
    }

    [Fact]
    public async Task PatchEstado_ReactivaEmpleado()
    {
        var creado = await CrearAsync(NuevoEmpleado());

        using var desactivar = await client.PatchAsJsonAsync(
            $"/api/empleados/{creado.Id}/estado", new { activo = false, fechaSalida = "2026-12-31" }, Json);
        var inactivo = await client.GetFromJsonAsync<EmpleadoResponse>($"/api/empleados/{creado.Id}", Json);
        using var activar = await client.PatchAsJsonAsync($"/api/empleados/{creado.Id}/estado", new { activo = true }, Json);
        var activo = await client.GetFromJsonAsync<EmpleadoResponse>($"/api/empleados/{creado.Id}", Json);

        Assert.Equal(HttpStatusCode.NoContent, desactivar.StatusCode);
        Assert.Equal(new DateOnly(2026, 12, 31), inactivo!.FechaSalida);
        Assert.Equal(HttpStatusCode.NoContent, activar.StatusCode);
        Assert.True(activo!.Activo);
        Assert.Null(activo.FechaSalida);
    }

    [Fact]
    public async Task GetPaises_RetornaCatalogoSembrado()
    {
        var json = await client.GetStringAsync("/api/paises");

        Assert.Contains("\"codigoIso3\":\"HND\"", json, StringComparison.Ordinal);
    }
}
