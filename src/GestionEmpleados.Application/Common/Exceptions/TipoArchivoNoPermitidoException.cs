namespace GestionEmpleados.Application.Common.Exceptions;

/// <summary>El tipo de archivo recibido no está permitido (HTTP 415).</summary>
public sealed class TipoArchivoNoPermitidoException : Exception
{
    public TipoArchivoNoPermitidoException()
    {
    }

    public TipoArchivoNoPermitidoException(string message)
        : base(message)
    {
    }

    public TipoArchivoNoPermitidoException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
