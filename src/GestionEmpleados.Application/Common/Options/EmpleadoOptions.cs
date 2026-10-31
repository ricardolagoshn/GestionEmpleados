using GestionEmpleados.Application.Utilities;

namespace GestionEmpleados.Application.Common.Options;

public sealed class EmpleadoOptions
{
    public const string SectionName = "EmpleadoOptions";

    public int EdadMinima { get; set; } = 18;
    public int FotografiaMaxBytes { get; set; } = Base64ImageConverter.MaxBytes;
}
