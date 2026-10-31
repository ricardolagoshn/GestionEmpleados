# GestionEmpleados

API REST para registrar, consultar, modificar y desactivar empleados, con catálogo de países y fotografía recibida como Base64 y almacenada como `VARBINARY(MAX)` en SQL Server.

| Elemento | Versión |
|---|---|
| .NET SDK | 10.0.402 (`global.json`, `rollForward: latestPatch`) |
| ASP.NET Core / EF Core | 10.0.13 |
| Base de datos | SQL Server 2019+ (probado con el contenedor `sql-empleados`) |

## Arquitectura

```text
GestionEmpleados.Api ──► Application ──► Domain
        └──────────────► Infrastructure ──► Application, Domain
```

- **Domain**: entidades `Empleado` y `Pais`, enum `Genero`, `DomainException`.
- **Application**: DTO, contratos, `EmpleadoService`, validaciones y `Base64ImageConverter` (firma JPEG/PNG/WebP).
- **Infrastructure**: `ApplicationDbContext`, configuraciones Fluent API, migraciones, seeding de países y repositorios.
- **Api**: controladores, `ExceptionHandlingMiddleware` (ProblemDetails), OpenAPI.

## Puesta en marcha

1. SQL Server en Docker:

   ```powershell
   docker run -d --name sql-empleados --hostname sql-empleados `
     -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<su-contraseña>" -e "MSSQL_PID=Developer" `
     -p 1433:1433 -v sql-empleados-data:/var/opt/mssql --restart unless-stopped <imagen>
   ```

2. Cadena de conexión en User Secrets (nunca en `appsettings.json`):

   ```powershell
   dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=GestionEmpleadosDb;User Id=sa;Password=<su-contraseña>;TrustServerCertificate=True;MultipleActiveResultSets=True" --project src/GestionEmpleados.Api
   ```

3. Herramienta EF y base de datos:

   ```powershell
   dotnet tool update --global dotnet-ef --version 10.0.13
   dotnet ef database update --project src/GestionEmpleados.Infrastructure --startup-project src/GestionEmpleados.Api --context ApplicationDbContext
   ```

4. Ejecutar:

   ```powershell
   dotnet run --project src/GestionEmpleados.Api --launch-profile http
   ```

   - API: `http://localhost:5227/api/empleados`
   - Scalar (Development): `http://localhost:5227/scalar/v1` — interfaz para explorar y probar la API
   - OpenAPI (Development): `http://localhost:5227/openapi/v1.json`
   - Ejemplos listos: `src/GestionEmpleados.Api/GestionEmpleados.Api.http`

## Pruebas

```powershell
dotnet build GestionEmpleados.slnx --configuration Release
dotnet test GestionEmpleados.slnx --configuration Release --no-build
dotnet format GestionEmpleados.slnx --verify-no-changes
```

Las pruebas de integración usan una base aislada, **`GestionEmpleadosDb_Tests`**, que se elimina y recrea con migraciones en cada ejecución. Toman el servidor y las credenciales de `ConnectionStrings:TestConnection` o, si no existe, de `ConnectionStrings:DefaultConnection` (User Secrets de la API o variables de entorno), y siempre reemplazan el nombre de la base. Nunca tocan la base de desarrollo.

## Endpoints

| Método | Ruta | Respuesta |
|---|---|---|
| POST | `/api/empleados` | 201 + `Location` |
| GET | `/api/empleados?search&paisId&genero&activo&page&pageSize&sortBy&sortDirection` | 200 paginado |
| GET | `/api/empleados/{id}` | 200 |
| PUT | `/api/empleados/{id}` (requiere `version`) | 200 |
| PATCH | `/api/empleados/{id}/estado` | 204 |
| DELETE | `/api/empleados/{id}` (lógico) | 204 |
| GET / PUT / DELETE | `/api/empleados/{id}/fotografia` | 200 imagen / 204 / 204 |
| GET | `/api/paises`, `/api/paises/{id}` | 200 |

Errores: `application/problem+json` con `400`, `404`, `409` (duplicados o concurrencia), `413`, `415` y `500`.

## Notas

- **Ruta larga en Windows:** si el repositorio está en una ruta de más de 100 caracteres (por ejemplo OneDrive), `Directory.Build.props` envía `bin/obj` a `%LOCALAPPDATA%\GestionEmpleados\artifacts`. Sin esto, la DLL nativa de SqlClient supera `MAX_PATH` y no carga.
- En producción, la API no aplica migraciones al iniciar. Genere el script idempotente con `dotnet ef migrations script --idempotent ... --output artifacts/GestionEmpleados.sql`.
