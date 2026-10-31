using GestionEmpleados.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionEmpleados.Infrastructure.Data.Configurations;

public sealed class EmpleadoConfiguration : IEntityTypeConfiguration<Empleado>
{
    public const string IndiceCodigo = "UX_Empleados_CodigoEmpleado";
    public const string IndiceCedula = "UX_Empleados_PaisId_Cedula";
    public const string IndiceCorreo = "UX_Empleados_CorreoElectronico";

    public void Configure(EntityTypeBuilder<Empleado> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Empleados", t =>
        {
            t.HasCheckConstraint("CK_Empleados_FechaSalida", "[FechaSalida] IS NULL OR [FechaSalida] >= [FechaIngreso]");
            t.HasCheckConstraint("CK_Empleados_Genero", "[Genero] BETWEEN 0 AND 3");
        });
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).UseIdentityColumn();
        builder.Property(x => x.CodigoEmpleado).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Nombres).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Apellidos).HasMaxLength(100).IsRequired();
        builder.Property(x => x.FechaNacimiento).HasColumnType("date");
        builder.Property(x => x.Cedula).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Direccion).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Genero).HasConversion<byte>().IsRequired();
        builder.Property(x => x.Telefono).HasMaxLength(25);
        builder.Property(x => x.CorreoElectronico).HasMaxLength(150);
        builder.Property(x => x.FechaIngreso).HasColumnType("date");
        builder.Property(x => x.FechaSalida).HasColumnType("date");
        builder.Property(x => x.Cargo).HasMaxLength(100);
        builder.Property(x => x.Fotografia).HasColumnType("varbinary(max)");
        builder.Property(x => x.FotografiaTipoContenido).HasMaxLength(50).IsUnicode(false);
        builder.Property(x => x.FotografiaNombreArchivo).HasMaxLength(255);
        builder.Property(x => x.Activo).HasDefaultValue(true).HasSentinel(true);
        builder.Property(x => x.FechaCreacionUtc).HasPrecision(0);
        builder.Property(x => x.FechaActualizacionUtc).HasPrecision(0);
        builder.Property(x => x.Version).IsRowVersion();

        builder.HasIndex(x => x.CodigoEmpleado).IsUnique().HasDatabaseName(IndiceCodigo);
        builder.HasIndex(x => new { x.PaisId, x.Cedula }).IsUnique().HasDatabaseName(IndiceCedula);
        builder.HasIndex(x => new { x.Apellidos, x.Nombres });
        builder.HasIndex(x => x.Activo);
        builder.HasIndex(x => x.CorreoElectronico)
            .IsUnique()
            .HasFilter("[CorreoElectronico] IS NOT NULL")
            .HasDatabaseName(IndiceCorreo);

        builder.HasOne(x => x.Pais)
            .WithMany(x => x.Empleados)
            .HasForeignKey(x => x.PaisId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
