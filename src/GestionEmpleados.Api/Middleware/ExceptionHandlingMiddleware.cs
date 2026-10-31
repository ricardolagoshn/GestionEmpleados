using GestionEmpleados.Application.Common.Exceptions;
using GestionEmpleados.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace GestionEmpleados.Api.Middleware;

/// <summary>
/// Convierte las excepciones en respuestas <c>application/problem+json</c> sin exponer detalles internos.
/// </summary>
public sealed partial class ExceptionHandlingMiddleware(
    RequestDelegate next,
    IProblemDetailsService problemDetailsService,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            LogSolicitudCancelada(logger, context.Request.Method, context.Request.Path);
        }
        catch (Exception exception)
        {
            if (context.Response.HasStarted)
            {
                LogRespuestaIniciada(logger, exception);
                throw;
            }

            var (status, title, detail) = Clasificar(exception);

            if (status >= StatusCodes.Status500InternalServerError)
            {
                LogErrorInesperado(logger, exception, context.Request.Method, context.Request.Path);
            }

            var problem = new ProblemDetails
            {
                Type = $"https://httpstatuses.com/{status}",
                Title = title,
                Status = status,
                Detail = detail,
                Instance = context.Request.Path
            };

            // Se agrega como extensión porque el escritor serializa el tipo base ProblemDetails.
            if (exception is ValidacionException { Errores.Count: > 0 } validacion)
            {
                problem.Extensions["errors"] = validacion.Errores;
            }

            context.Response.Clear();
            context.Response.StatusCode = status;

            await problemDetailsService.WriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = problem
            });
        }
    }

    private static (int Status, string Title, string? Detail) Clasificar(Exception exception) => exception switch
    {
        ValidacionException e => (StatusCodes.Status400BadRequest, "Datos no válidos", e.Message),
        DomainException e => (StatusCodes.Status400BadRequest, "Regla de negocio no cumplida", e.Message),
        NotFoundException e => (StatusCodes.Status404NotFound, "Recurso no encontrado", e.Message),
        ConflictException e => (StatusCodes.Status409Conflict, "Conflicto de datos", e.Message),
        ArchivoDemasiadoGrandeException e => (StatusCodes.Status413PayloadTooLarge, "Contenido demasiado grande", e.Message),
        TipoArchivoNoPermitidoException e => (StatusCodes.Status415UnsupportedMediaType, "Tipo de archivo no permitido", e.Message),
        BadHttpRequestException { StatusCode: StatusCodes.Status413PayloadTooLarge } =>
            (StatusCodes.Status413PayloadTooLarge, "Contenido demasiado grande", "La solicitud supera el tamaño permitido."),
        BadHttpRequestException e => (e.StatusCode, "Solicitud no válida", null),
        _ => (StatusCodes.Status500InternalServerError, "Error interno", "Ocurrió un error inesperado. Intente de nuevo más tarde.")
    };

    [LoggerMessage(Level = LogLevel.Error, Message = "Error no controlado en {Method} {Path}")]
    private static partial void LogErrorInesperado(ILogger logger, Exception exception, string method, PathString path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Error después de iniciar la respuesta; no se puede generar ProblemDetails")]
    private static partial void LogRespuestaIniciada(ILogger logger, Exception exception);

    [LoggerMessage(Level = LogLevel.Information, Message = "Solicitud cancelada por el cliente: {Method} {Path}")]
    private static partial void LogSolicitudCancelada(ILogger logger, string method, PathString path);
}
