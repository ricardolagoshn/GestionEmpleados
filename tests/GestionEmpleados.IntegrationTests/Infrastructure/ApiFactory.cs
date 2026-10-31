using GestionEmpleados.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GestionEmpleados.IntegrationTests.Infrastructure;

/// <summary>
/// Levanta la API contra una base exclusiva de pruebas (<c>GestionEmpleadosDb_Tests</c>) recreada con migraciones.
/// El servidor y las credenciales se toman de <c>ConnectionStrings:TestConnection</c> o, en su defecto,
/// de <c>ConnectionStrings:DefaultConnection</c> (User Secrets de la API o variables de entorno).
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string BaseDeDatos = "GestionEmpleadosDb_Tests";

    private readonly string connectionString = ObtenerCadenaDePruebas();

    async Task IAsyncLifetime.InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
            new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = connectionString }));
    }

    private static string ObtenerCadenaDePruebas()
    {
        var config = new ConfigurationBuilder()
            .AddUserSecrets<Program>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cadena = config.GetConnectionString("TestConnection") ?? config.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(cadena))
        {
            throw new InvalidOperationException(
                "Configure ConnectionStrings:TestConnection o ConnectionStrings:DefaultConnection para las pruebas de integración.");
        }

        // Se fuerza una base exclusiva para no tocar nunca la base de desarrollo.
        return new SqlConnectionStringBuilder(cadena) { InitialCatalog = BaseDeDatos }.ConnectionString;
    }
}

[CollectionDefinition(Nombre)]
public sealed class ApiTestGroup : ICollectionFixture<ApiFactory>
{
    public const string Nombre = "Api";
}
