using GestionEmpleados.Api.Extensions;
using GestionEmpleados.Application.Common.Models;
using GestionEmpleados.Application.DTOs.Empleados;
using GestionEmpleados.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GestionEmpleados.Api.Controllers;

[ApiController]
[Route("api/empleados")]
public sealed class EmpleadosController(IEmpleadoService empleadoService) : ControllerBase
{
    [HttpPost]
    [RequestSizeLimit(ServiceCollectionExtensions.LimiteSolicitudFotografiaBytes)]
    [ProducesResponseType<EmpleadoResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    public async Task<ActionResult<EmpleadoResponse>> CrearAsync(
        [FromBody] CrearEmpleadoRequest request,
        CancellationToken cancellationToken)
    {
        var empleado = await empleadoService.CrearAsync(request, cancellationToken);
        return CreatedAtAction(nameof(ObtenerAsync), new { id = empleado.Id }, empleado);
    }

    [HttpGet]
    [ProducesResponseType<PaginatedResult<EmpleadoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    public async Task<ActionResult<PaginatedResult<EmpleadoResponse>>> BuscarAsync(
        [FromQuery] BuscarEmpleadosRequest filtro,
        CancellationToken cancellationToken) =>
        Ok(await empleadoService.BuscarAsync(filtro, cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<EmpleadoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<EmpleadoResponse>> ObtenerAsync(int id, CancellationToken cancellationToken) =>
        Ok(await empleadoService.ObtenerAsync(id, cancellationToken));

    [HttpPut("{id:int}")]
    [ProducesResponseType<EmpleadoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<ActionResult<EmpleadoResponse>> ActualizarAsync(
        int id,
        [FromBody] ActualizarEmpleadoRequest request,
        CancellationToken cancellationToken) =>
        Ok(await empleadoService.ActualizarAsync(id, request, cancellationToken));

    [HttpPatch("{id:int}/estado")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> CambiarEstadoAsync(
        int id,
        [FromBody] CambiarEstadoRequest request,
        CancellationToken cancellationToken)
    {
        await empleadoService.CambiarEstadoAsync(id, request, cancellationToken);
        return NoContent();
    }

    /// <summary>Desactivación lógica: el registro se conserva con <c>activo = false</c>.</summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> DesactivarAsync(int id, CancellationToken cancellationToken)
    {
        await empleadoService.DesactivarAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:int}/fotografia")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, "image/jpeg", "image/png", "image/webp")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> ObtenerFotografiaAsync(int id, CancellationToken cancellationToken)
    {
        var foto = await empleadoService.ObtenerFotografiaAsync(id, cancellationToken);
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(foto.Contenido, foto.TipoContenido);
    }

    [HttpPut("{id:int}/fotografia")]
    [RequestSizeLimit(ServiceCollectionExtensions.LimiteSolicitudFotografiaBytes)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status415UnsupportedMediaType, "application/problem+json")]
    public async Task<IActionResult> ActualizarFotografiaAsync(
        int id,
        [FromBody] ActualizarFotografiaRequest request,
        CancellationToken cancellationToken)
    {
        await empleadoService.ActualizarFotografiaAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}/fotografia")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> EliminarFotografiaAsync(int id, CancellationToken cancellationToken)
    {
        await empleadoService.EliminarFotografiaAsync(id, cancellationToken);
        return NoContent();
    }
}
