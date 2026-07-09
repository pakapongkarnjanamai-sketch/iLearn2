namespace iLearn.Application.Common
{
    public class EmployeeServiceSettings
    {
        public string Provider             { get; set; } = "Legacy"; // "Legacy" | "EmployeeHub"
        public string EmployeeHubBaseUrl   { get; set; } = string.Empty;
        public string BaseLearnerLookupUrl { get; set; } = string.Empty;
        public string BaseLearnerUrl       { get; set; } = string.Empty;
        public string BaseEmployeeCsvUrl   { get; set; } = string.Empty;
    }
}
