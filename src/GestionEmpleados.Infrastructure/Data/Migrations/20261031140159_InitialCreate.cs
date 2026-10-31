using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace GestionEmpleados.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Paises",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoIso2 = table.Column<string>(type: "char(2)", unicode: false, fixedLength: true, maxLength: 2, nullable: false),
                    CodigoIso3 = table.Column<string>(type: "char(3)", unicode: false, fixedLength: true, maxLength: 3, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Paises", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Empleados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CodigoEmpleado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombres = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Apellidos = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaNacimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    Cedula = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PaisId = table.Column<int>(type: "int", nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Genero = table.Column<byte>(type: "tinyint", nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(25)", maxLength: 25, nullable: true),
                    CorreoElectronico = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    FechaIngreso = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaSalida = table.Column<DateOnly>(type: "date", nullable: true),
                    Cargo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Fotografia = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    FotografiaTipoContenido = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    FotografiaNombreArchivo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    FechaCreacionUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    FechaActualizacionUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    Version = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empleados", x => x.Id);
                    table.CheckConstraint("CK_Empleados_FechaSalida", "[FechaSalida] IS NULL OR [FechaSalida] >= [FechaIngreso]");
                    table.CheckConstraint("CK_Empleados_Genero", "[Genero] BETWEEN 0 AND 3");
                    table.ForeignKey(
                        name: "FK_Empleados_Paises_PaisId",
                        column: x => x.PaisId,
                        principalTable: "Paises",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Paises",
                columns: new[] { "Id", "Activo", "CodigoIso2", "CodigoIso3", "Nombre" },
                values: new object[,]
                {
                    { 1, true, "HN", "HND", "Honduras" },
                    { 2, true, "GT", "GTM", "Guatemala" },
                    { 3, true, "SV", "SLV", "El Salvador" },
                    { 4, true, "NI", "NIC", "Nicaragua" },
                    { 5, true, "CR", "CRI", "Costa Rica" },
                    { 6, true, "PA", "PAN", "Panamá" },
                    { 7, true, "MX", "MEX", "México" },
                    { 8, true, "US", "USA", "Estados Unidos" },
                    { 9, true, "CO", "COL", "Colombia" },
                    { 10, true, "ES", "ESP", "España" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_Activo",
                table: "Empleados",
                column: "Activo");

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_Apellidos_Nombres",
                table: "Empleados",
                columns: new[] { "Apellidos", "Nombres" });

            migrationBuilder.CreateIndex(
                name: "UX_Empleados_CodigoEmpleado",
                table: "Empleados",
                column: "CodigoEmpleado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_Empleados_CorreoElectronico",
                table: "Empleados",
                column: "CorreoElectronico",
                unique: true,
                filter: "[CorreoElectronico] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UX_Empleados_PaisId_Cedula",
                table: "Empleados",
                columns: new[] { "PaisId", "Cedula" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Paises_CodigoIso2",
                table: "Paises",
                column: "CodigoIso2",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Paises_CodigoIso3",
                table: "Paises",
                column: "CodigoIso3",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Paises_Nombre",
                table: "Paises",
                column: "Nombre",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Empleados");

            migrationBuilder.DropTable(
                name: "Paises");
        }
    }
}
