using iLearn.Application.Common;
using iLearn.Application.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace iLearn.API.Controllers
{
    [Route("api/admin/[controller]")]
    [ApiController]
    public class SystemConfigController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly FileSettings _fileSettings;
        private readonly EmployeeServiceSettings _employeeSettings;

        public SystemConfigController(
            IConfiguration config,
            IWebHostEnvironment env,
            IOptions<FileSettings> fileSettings,
            IOptions<EmployeeServiceSettings> employeeSettings)
        {
            _config           = config;
            _env              = env;
            _fileSettings     = fileSettings.Value;
            _employeeSettings = employeeSettings.Value;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var rawConn = _config.GetConnectionString("DefaultConnection") ?? "";

            // Parse connection string parts safely
            var connParts = ParseConnectionString(rawConn);

            return Ok(new
            {
                environment = _env.EnvironmentName,
                database = new
                {
                    dataSource   = connParts.GetValueOrDefault("Data Source") ?? connParts.GetValueOrDefault("Server", "(not set)"),
                    databaseName = connParts.GetValueOrDefault("Database") ?? connParts.GetValueOrDefault("Initial Catalog", "(not set)"),
                    userId       = connParts.GetValueOrDefault("User ID") ?? connParts.GetValueOrDefault("UID", "(not set)"),
                    trustCert    = connParts.GetValueOrDefault("Trust Server Certificate", "false"),
                },
                fileSettings = new
                {
                    hostUrl      = _fileSettings.HostUrl,
                    hostUnc      = _fileSettings.HostUnc,
                    courseFolder = _fileSettings.CourseFolder,
                    fileUrl      = _fileSettings.FileUrl,
                    fileUnc      = _fileSettings.FileUnc,
                },
                employeeService = new
                {
                    baseStudentLookupUrl = _employeeSettings.BaseStudentLookupUrl,
                    baseStudentUrl       = _employeeSettings.BaseStudentUrl,
                },
                allowedHosts = _config["AllowedHosts"] ?? "*",
                logging = _config.GetSection("Logging:LogLevel")
                    .GetChildren()
                    .ToDictionary(x => x.Key, x => x.Value ?? ""),
                runtime = new
                {
                    dotNetVersion  = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                    machineName    = System.Environment.MachineName,
                    osDescription  = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                    osArchitecture = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
                    serverTime     = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    appVersion     = typeof(SystemConfigController).Assembly.GetName().Version?.ToString() ?? "1.0.0",
                }
            });
        }

        private static Dictionary<string, string> ParseConnectionString(string connStr)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var part in connStr.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var idx = part.IndexOf('=');
                if (idx > 0)
                    dict[part[..idx].Trim()] = part[(idx + 1)..].Trim();
            }
            return dict;
        }
    }
}
