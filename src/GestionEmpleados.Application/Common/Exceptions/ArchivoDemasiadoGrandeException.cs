namespace GestionEmpleados.Application.Common.Exceptions;

/// <summary>El archivo recibido supera el tamaño permitido (HTTP 413).</summary>
public sealed class ArchivoDemasiadoGrandeException : Exception
{
    public ArchivoDemasiadoGrandeException()
    {
    }

    public ArchivoDemasiadoGrandeException(string message)
        : base(message)
    {
    }

    public ArchivoDemasiadoGrandeException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
