using GestionEmpleados.Domain.Entities;
using GestionEmpleados.Infrastructure.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionEmpleados.Infrastructure.Data.Configurations;

public sealed class PaisConfiguration : IEntityTypeConfiguration<Pais>
{
    public void Configure(EntityTypeBuilder<Pais> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Paises");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).UseIdentityColumn();
        builder.Property(x => x.CodigoIso2).HasMaxLength(2).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(x => x.CodigoIso3).HasMaxLength(3).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(x => x.Nombre).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Activo).HasDefaultValue(true).HasSentinel(true);

        builder.HasIndex(x => x.CodigoIso2).IsUnique();
        builder.HasIndex(x => x.CodigoIso3).IsUnique();
        builder.HasIndex(x => x.Nombre).IsUnique();

        builder.HasData(PaisSeeder.Paises);
    }
}
