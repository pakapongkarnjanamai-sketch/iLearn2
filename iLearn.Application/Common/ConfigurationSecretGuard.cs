namespace iLearn.Application.Common
{
    public static class ConfigurationSecretGuard
    {
        public const string PlaceholderPrefix = "__SET_";

        public static bool HasRealValue(string? value)
        {
            return !string.IsNullOrWhiteSpace(value)
                && !value.Trim().StartsWith(PlaceholderPrefix, StringComparison.OrdinalIgnoreCase);
        }

        public static string ToEnvironmentVariableName(string configurationKey)
        {
            return configurationKey.Replace(':', '_').Replace("__", "_");
        }
    }
}