namespace GestionEmpleados.Application.Common.Exceptions;

/// <summary>Uno o más datos de entrada no cumplen las reglas (HTTP 400).</summary>
public sealed class ValidacionException : Exception
{
    private const string MensajePredeterminado = "Uno o más datos no son válidos.";

    public ValidacionException()
        : this(MensajePredeterminado)
    {
    }

    public ValidacionException(string message)
        : base(message)
    {
        Errores = new Dictionary<string, string[]>();
    }

    public ValidacionException(string message, Exception innerException)
        : base(message, innerException)
    {
        Errores = new Dictionary<string, string[]>();
    }

    public ValidacionException(string campo, string mensaje)
        : this(new Dictionary<string, string[]> { [campo] = [mensaje] })
    {
    }

    public ValidacionException(IReadOnlyDictionary<string, string[]> errores)
        : base(MensajePredeterminado)
    {
        Errores = errores;
    }

    public IReadOnlyDictionary<string, string[]> Errores { get; }
}
