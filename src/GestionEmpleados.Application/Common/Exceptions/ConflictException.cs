namespace GestionEmpleados.Application.Common.Exceptions;

/// <summary>Datos duplicados o conflicto de concurrencia (HTTP 409).</summary>
public sealed class ConflictException : Exception
{
    public ConflictException()
    {
    }

    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
