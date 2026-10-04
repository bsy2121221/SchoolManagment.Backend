using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using SchoolManagment.DataAccess.Context;

namespace SchoolManagment.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HealthController : ControllerBase
    {
        private readonly IDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public HealthController(IDbContext dbContext, IConfiguration configuration, IWebHostEnvironment webHostEnvironment)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "ok",
                environment = _webHostEnvironment.EnvironmentName,
                timestamp = DateTime.UtcNow
            }
                );
        }

        [HttpGet("Database")]
        public async Task<IActionResult> GetDatabase(CancellationToken cancellationToken)
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return StatusCode(503, new
                {
                    status = "error",
                    database = "not_configured",
                    message = "Set ConnectionStrings__DefaultConnection in App Service application settings, or DefaultConnection under Connection strings (SQLAzure)."
                });
            }

            var connectionBuilder = new SqlConnectionStringBuilder(connectionString);
            if (connectionBuilder.DataSource.Contains("SQLEXPRESS", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(503, new
                {
                    status = "error",
                    database = "wrong_config",
                    server = connectionBuilder.DataSource,
                    catalog = connectionBuilder.InitialCatalog,
                    message = "App is using the local SQL Express connection from appsettings.json. Set ConnectionStrings__DefaultConnection in Azure App Service."
                });
            }

            try
            {
                await using var connection = (SqlConnection)_dbContext.CreateConnection();
                await connection.OpenAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                await command.ExecuteScalarAsync(cancellationToken);

                var builder = new SqlConnectionStringBuilder(connectionString);

                return Ok(new
                {
                    status = "ok",
                    database = "connected",
                    server = builder.DataSource,
                    catalog = builder.InitialCatalog
                });
            }
            catch (Exception ex)
            {
                var builder = new SqlConnectionStringBuilder(connectionString);
                var sqlError = ex as SqlException ?? ex.InnerException as SqlException;

                return StatusCode(503, new
                {
                    status = "error",
                    database = "connection_failed",
                    server = builder.DataSource,
                    catalog = builder.InitialCatalog,
                    sqlErrorNumber = sqlError?.Number,
                    message = _webHostEnvironment.IsDevelopment()
                        ? ex.Message
                        : sqlError?.Number switch
                        {
                            18456 => "SQL login failed. Check username and password in the App Service connection string.",
                            4060 => "Cannot open database. Verify the database name exists on the SQL server.",
                            -2 or 53 or 11001 => "Network or firewall issue. Allow Azure services on the SQL server firewall.",
                            _ => "Unable to connect to the database. Check Azure App Service connection string and SQL firewall."
                        }
                });
            }
        }


    }
}
