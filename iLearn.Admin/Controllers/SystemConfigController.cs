using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Text.Json;

namespace iLearn.Admin.Controllers
{
    [Authorize(Policy = "SuperAdminOnly")]
    public class SystemConfigController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SystemConfigController> _logger;

        public SystemConfigController(
            IConfiguration config,
            IWebHostEnvironment env,
            IHttpClientFactory httpClientFactory,
            ILogger<SystemConfigController> logger)
        {
            _config = config;
            _env = env;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new SystemConfigViewModel
            {
                AdminEnvironment = _env.EnvironmentName,
                AdminApiBaseUrl = _config["ApiSettings:BaseUrl"] ?? "(not set)",
                AdminAllowedHosts = _config["AllowedHosts"] ?? "*",
            };

            foreach (var item in _config.GetSection("Logging:LogLevel").GetChildren())
                vm.AdminLogLevels[item.Key] = item.Value ?? "";

            if (!await TryLoadApiConfigAsync(vm))
            {
                LoadApiConfigFromPublishedFiles(vm);
            }

            return View(vm);
        }

        private async Task<bool> TryLoadApiConfigAsync(SystemConfigViewModel vm)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("iLearnAPI");
                var resp = await client.GetAsync("admin/SystemConfig");
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("SystemConfig API call failed with status code {StatusCode}", resp.StatusCode);
                    return false;
                }

                var json = await resp.Content.ReadAsStringAsync();
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var doc = JsonSerializer.Deserialize<JsonElement>(json, opts);

                PopulateFromApiResponse(vm, doc);
                vm.ApiReachable = true;
                vm.ApiSource = "API";
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to load system configuration from API");
                return false;
            }
        }

        private void PopulateFromApiResponse(SystemConfigViewModel vm, JsonElement doc)
        {
            vm.ApiEnvironment = doc.TryGetProp("environment");
            vm.ApiAllowedHosts = doc.TryGetProp("allowedHosts");

            vm.Database = new DbConfigInfo
            {
                DataSource = doc.GetNestedProp("database", "dataSource"),
                DatabaseName = doc.GetNestedProp("database", "databaseName"),
                UserId = doc.GetNestedProp("database", "userId"),
                TrustCert = doc.GetNestedProp("database", "trustCert"),
            };

            vm.FileSettings = new FileSettingsInfo
            {
                HostUrl = doc.GetNestedProp("fileSettings", "hostUrl"),
                HostUnc = doc.GetNestedProp("fileSettings", "hostUnc"),
                CourseFolder = doc.GetNestedProp("fileSettings", "courseFolder"),
                FileUrl = doc.GetNestedProp("fileSettings", "fileUrl"),
                FileUnc = doc.GetNestedProp("fileSettings", "fileUnc"),
            };

            vm.EmployeeService = new EmployeeServiceInfo
            {
                BaseLearnerLookupUrl = doc.GetNestedProp("employeeService", "baseLearnerLookupUrl"),
                BaseLearnerUrl = doc.GetNestedProp("employeeService", "baseLearnerUrl"),
            };

            vm.ApiRuntime = new ApiRuntimeInfo
            {
                DotNetVersion = doc.GetNestedProp("runtime", "dotNetVersion"),
                MachineName = doc.GetNestedProp("runtime", "machineName"),
                OsDescription = doc.GetNestedProp("runtime", "osDescription"),
                OsArchitecture = doc.GetNestedProp("runtime", "osArchitecture"),
                ServerTime = doc.GetNestedProp("runtime", "serverTime"),
                AppVersion = doc.GetNestedProp("runtime", "appVersion"),
            };

            if (doc.TryGetProperty("logging", out var logging) && logging.ValueKind == JsonValueKind.Object)
            {
                vm.ApiLogLevels = logging.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.GetString() ?? "");
            }
        }

        private void LoadApiConfigFromPublishedFiles(SystemConfigViewModel vm)
        {
            try
            {
                var serviceRoot = Path.GetFullPath(Path.Combine(_env.ContentRootPath, "..", "Service"));
                if (!Directory.Exists(serviceRoot))
                    return;

                var apiConfig = new ConfigurationBuilder()
                    .SetBasePath(serviceRoot)
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
                    .AddJsonFile($"appsettings.{_env.EnvironmentName}.json", optional: true, reloadOnChange: false)
                    .Build();

                var connectionString = apiConfig.GetConnectionString("DefaultConnection") ?? "";
                var connParts = ParseConnectionString(connectionString);

                vm.ApiEnvironment = _env.EnvironmentName;
                vm.ApiAllowedHosts = apiConfig["AllowedHosts"] ?? "*";
                vm.ApiLogLevels = apiConfig.GetSection("Logging:LogLevel")
                    .GetChildren()
                    .ToDictionary(x => x.Key, x => x.Value ?? "");

                var hostUrl = apiConfig["FileSettings:HostUrl"] ?? "";
                var hostUnc = apiConfig["FileSettings:HostUnc"] ?? "";
                var courseFolder = apiConfig["FileSettings:CourseFolder"] ?? "";

                vm.Database = new DbConfigInfo
                {
                    DataSource = connParts.GetValueOrDefault("Data Source") ?? connParts.GetValueOrDefault("Server", ""),
                    DatabaseName = connParts.GetValueOrDefault("Database") ?? connParts.GetValueOrDefault("Initial Catalog", ""),
                    UserId = connParts.GetValueOrDefault("User ID") ?? connParts.GetValueOrDefault("UID", ""),
                    TrustCert = connParts.GetValueOrDefault("Trust Server Certificate", "false"),
                };

                vm.FileSettings = new FileSettingsInfo
                {
                    HostUrl = hostUrl,
                    HostUnc = hostUnc,
                    CourseFolder = courseFolder,
                    FileUrl = CombineUrl(hostUrl, courseFolder),
                    FileUnc = CombineUnc(hostUnc, courseFolder),
                };

                vm.EmployeeService = new EmployeeServiceInfo
                {
                    BaseLearnerLookupUrl = apiConfig["EmployeeServiceSettings:BaseLearnerLookupUrl"] ?? "",
                    BaseLearnerUrl = apiConfig["EmployeeServiceSettings:BaseLearnerUrl"] ?? "",
                };

                vm.ApiRuntime = new ApiRuntimeInfo
                {
                    DotNetVersion = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                    MachineName = Environment.MachineName,
                    OsDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
                    OsArchitecture = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture.ToString(),
                    ServerTime = "-",
                    AppVersion = GetApiAssemblyVersion(serviceRoot)
                };

                vm.ApiSource = "Published Files";
                vm.ApiConfigFallbackUsed = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to load system configuration from published API files");
            }
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

        private static string CombineUrl(string baseUrl, string child)
        {
            if (string.IsNullOrWhiteSpace(baseUrl))
                return string.Empty;

            return string.IsNullOrWhiteSpace(child)
                ? baseUrl.TrimEnd('/')
                : $"{baseUrl.TrimEnd('/')}/{child.Trim('/')}";
        }

        private static string CombineUnc(string basePath, string child)
        {
            if (string.IsNullOrWhiteSpace(basePath))
                return string.Empty;

            return string.IsNullOrWhiteSpace(child)
                ? basePath.TrimEnd('\\')
                : Path.Combine(basePath, child);
        }

        private static string GetApiAssemblyVersion(string serviceRoot)
        {
            try
            {
                var assemblyPath = Path.Combine(serviceRoot, "iLearn.API.dll");
                return System.IO.File.Exists(assemblyPath)
                    ? AssemblyName.GetAssemblyName(assemblyPath).Version?.ToString() ?? string.Empty
                    : string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    // ?? Sub-models (deserialized from API response) ??
    public class DbConfigInfo
    {
        public string DataSource { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string TrustCert { get; set; } = string.Empty;
    }

    public class FileSettingsInfo
    {
        public string HostUrl { get; set; } = string.Empty;
        public string HostUnc { get; set; } = string.Empty;
        public string CourseFolder { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string FileUnc { get; set; } = string.Empty;
    }

    public class ApiRuntimeInfo
    {
        public string DotNetVersion { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string OsDescription { get; set; } = string.Empty;
        public string OsArchitecture { get; set; } = string.Empty;
        public string ServerTime { get; set; } = string.Empty;
        public string AppVersion { get; set; } = string.Empty;
    }

    public class EmployeeServiceInfo
    {
        public string BaseLearnerLookupUrl { get; set; } = string.Empty;
        public string BaseLearnerUrl { get; set; } = string.Empty;
    }

    public class SystemConfigViewModel
    {
        // Admin-side
        public string AdminEnvironment { get; set; } = string.Empty;
        public string AdminApiBaseUrl { get; set; } = string.Empty;
        public string AdminAllowedHosts { get; set; } = string.Empty;
        public Dictionary<string, string> AdminLogLevels { get; set; } = new();

        // API-side (fetched from API)
        public bool ApiReachable { get; set; }
        public bool ApiConfigFallbackUsed { get; set; }
        public string ApiSource { get; set; } = string.Empty;
        public string ApiEnvironment { get; set; } = string.Empty;
        public string ApiAllowedHosts { get; set; } = string.Empty;
        public Dictionary<string, string> ApiLogLevels { get; set; } = new();
        public DbConfigInfo Database { get; set; } = new();
        public FileSettingsInfo FileSettings { get; set; } = new();
        public EmployeeServiceInfo EmployeeService { get; set; } = new();
        public ApiRuntimeInfo ApiRuntime { get; set; } = new();
        public bool HasApiData => ApiReachable || ApiConfigFallbackUsed;
    }

    // ?? JsonElement helpers ??
    internal static class JsonElementExtensions
    {
        public static string TryGetProp(this JsonElement el, string name)
            => el.TryGetProperty(name, out var p) ? p.GetString() ?? "" : "";

        public static string GetNestedProp(this JsonElement el, string section, string name)
            => el.TryGetProperty(section, out var sec) ? sec.TryGetProp(name) : "";
    }
}
