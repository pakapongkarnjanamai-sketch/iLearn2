using System.Text.RegularExpressions;

namespace iLearn.Application.Common
{
    public static class NameHelper
    {
        // Regex matching gender-indicating prefixes in Thai and English at the start of a name string.
        // Thai: นาย, นางสาว, นาง สาว, นาง, น.ส., น.ส, เด็กชาย, เด็กหญิง, ด.ช., ด.ญ.
        // English: Mr., Mr, Mrs., Mrs, Miss., Miss, Ms., Ms, Master., Master
        private static readonly Regex GenderPrefixRegex = new(
            @"^(?:นาง\s*สาว|น\.?\s*ส\.?|เด็กชาย|เด็กหญิง|ด\.?\s*ช\.?|ด\.?\s*ญ\.?|นาย|นาง|(?:\b(?:Master|Miss|Mrs|Ms|Mr)\b\.?))\s*",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        /// <summary>
        /// Removes gender-indicating title prefixes (e.g., นาย, นาง, นางสาว, น.ส., Mr., Mrs., Miss, Ms.)
        /// from the start of a name string.
        /// </summary>
        public static string StripGenderPrefix(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            var trimmed = name.Trim();
            var cleaned = GenderPrefixRegex.Replace(trimmed, string.Empty).Trim();
            return string.IsNullOrWhiteSpace(cleaned) ? trimmed : cleaned;
        }
    }
}
