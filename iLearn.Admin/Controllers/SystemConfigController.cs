using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace iLearn.Admin.Controllers
{
    // ?? Sub-models (deserialized from API response) ??
    public class DbConfigInfo
    {
        public string DataSource   { get; set; } = string.Empty;
        public string DatabaseName { get; set; } = string.Empty;
        public string UserId       { get; set; } = string.Empty;
        public string TrustCert    { get; set; } = string.Empty;
    }

    public class FileSettingsInfo
    {
        public string HostUrl      { get; set; } = string.Empty;
        public string HostUnc      { get; set; } = string.Empty;
        public string CourseFolder { get; set; } = string.Empty;
        public string FileUrl      { get; set; } = string.Empty;
        public string FileUnc      { get; set; } = string.Empty;
    }

    public class ApiRuntimeInfo
    {
        public string DotNetVersion  { get; set; } = string.Empty;
        public string MachineName    { get; set; } = string.Empty;
        public string OsDescription  { get; set; } = string.Empty;
        public string OsArchitecture { get; set; } = string.Empty;
        public string ServerTime     { get; set; } = string.Empty;
        public string AppVersion     { get; set; } = string.Empty;
    }

    public class EmployeeServiceInfo
    {
        public string BaseStudentLookupUrl { get; set; } = string.Empty;
        public string BaseStudentUrl       { get; set; } = string.Empty;
    }

    public class SystemConfigViewModel
    {
        // Admin-side
        public string AdminEnvironment  { get; set; } = string.Empty;
        public string AdminApiBaseUrl   { get; set; } = string.Empty;
        public string AdminAllowedHosts { get; set; } = string.Empty;
        public Dictionary<string, string> AdminLogLevels { get; set; } = new();

        // API-side (fetched from API)
        public bool   ApiReachable   { get; set; }
        public string ApiEnvironment { get; set; } = string.Empty;
        public string ApiAllowedHosts{ get; set; } = string.Empty;
        public Dictionary<string, string> ApiLogLevels  { get; set; } = new();
        public DbConfigInfo       Database        { get; set; } = new();
        public FileSettingsInfo   FileSettings    { get; set; } = new();
        public EmployeeServiceInfo EmployeeService { get; set; } = new();
        public ApiRuntimeInfo     ApiRuntime      { get; set; } = new();
    }

    public class SystemConfigController : Controller
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        private readonly IHttpClientFactory _httpClientFactory;

        public SystemConfigController(
            IConfiguration config,
            IWebHostEnvironment env,
            IHttpClientFactory httpClientFactory)
        {
            _config = config;
            _env = env;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<IActionResult> Index()
        {
            var vm = new SystemConfigViewModel
            {
                AdminEnvironment  = _env.EnvironmentName,
                AdminApiBaseUrl   = _config["ApiSettings:BaseUrl"] ?? "(not set)",
                AdminAllowedHosts = _config["AllowedHosts"] ?? "*",
            };

            // Admin logging levels
            foreach (var item in _config.GetSection("Logging:LogLevel").GetChildren())
                vm.AdminLogLevels[item.Key] = item.Value ?? "";

            // Fetch from API
            try
            {
                var client = _httpClientFactory.CreateClient("iLearnAPI");
                var resp = await client.GetAsync("admin/SystemConfig");
                if (resp.IsSuccessStatusCode)
                {
                    var json = await resp.Content.ReadAsStringAsync();
                    var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var doc  = JsonSerializer.Deserialize<JsonElement>(json, opts);

                    vm.ApiReachable   = true;
                    vm.ApiEnvironment = doc.TryGetProp("environment");
                    vm.ApiAllowedHosts= doc.TryGetProp("allowedHosts");

                    vm.Database = new DbConfigInfo
                    {
                        DataSource   = doc.GetNestedProp("database", "dataSource"),
                        DatabaseName = doc.GetNestedProp("database", "databaseName"),
                        UserId       = doc.GetNestedProp("database", "userId"),
                        TrustCert    = doc.GetNestedProp("database", "trustCert"),
                    };

                    vm.FileSettings = new FileSettingsInfo
                    {
                        HostUrl      = doc.GetNestedProp("fileSettings", "hostUrl"),
                        HostUnc      = doc.GetNestedProp("fileSettings", "hostUnc"),
                        CourseFolder = doc.GetNestedProp("fileSettings", "courseFolder"),
                        FileUrl      = doc.GetNestedProp("fileSettings", "fileUrl"),
                        FileUnc      = doc.GetNestedProp("fileSettings", "fileUnc"),
                    };

                    vm.EmployeeService = new EmployeeServiceInfo
                    {
                        BaseStudentLookupUrl = doc.GetNestedProp("employeeService", "baseStudentLookupUrl"),
                        BaseStudentUrl       = doc.GetNestedProp("employeeService", "baseStudentUrl"),
                    };

                    vm.ApiRuntime = new ApiRuntimeInfo
                    {
                        MachineName    = doc.GetNestedProp("runtime", "machineName"),
                        OsDescription  = doc.GetNestedProp("runtime", "osDescription"),
                        OsArchitecture = doc.GetNestedProp("runtime", "osArchitecture"),
                        DotNetVersion  = doc.GetNestedProp("runtime", "dotNetVersion"),
                        AppVersion     = doc.GetNestedProp("runtime", "appVersion"),
                        ServerTime     = doc.GetNestedProp("runtime", "serverTime"),
                    };

                    if (doc.TryGetProperty("logging", out var logEl))
                        foreach (var p in logEl.EnumerateObject())
                            vm.ApiLogLevels[p.Name] = p.Value.GetString() ?? "";
                }
            }
            catch
            {
                vm.ApiReachable = false;
            }

            return View(vm);
        }
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
