using GestionEmpleados.Application.DTOs.Paises;
using GestionEmpleados.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GestionEmpleados.Api.Controllers;

[ApiController]
[Route("api/paises")]
public sealed class PaisesController(IPaisService paisService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PaisResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PaisResponse>>> ListarAsync(CancellationToken cancellationToken) =>
        Ok(await paisService.ListarActivosAsync(cancellationToken));

    [HttpGet("{id:int}")]
    [ProducesResponseType<PaisResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<PaisResponse>> ObtenerAsync(int id, CancellationToken cancellationToken) =>
        Ok(await paisService.ObtenerAsync(id, cancellationToken));
}
