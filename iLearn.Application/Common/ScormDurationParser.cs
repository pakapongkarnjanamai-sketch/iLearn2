using System.Xml;

namespace iLearn.Application.Common
{
    /// <summary>
    /// Parses SCORM duration strings (both 1.2 timespan and 2004 ISO8601) to seconds,
    /// and converts seconds back to the appropriate format.
    /// </summary>
    public static class ScormDurationParser
    {
        /// <summary>
        /// Converts a SCORM duration string to total seconds (rounded).
        /// Supports SCORM 1.2 timespan "HHHH:MM:SS(.cc)" and SCORM 2004 ISO8601 "P[nD]T[nH][nM][n(.n)S]".
        /// Returns 0 for null, empty, or unparseable values.
        /// </summary>
        public static int ToSeconds(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            var trimmed = value.Trim();

            // Try SCORM 1.2 timespan format: H...H:MM:SS or H...H:MM:SS.cc
            if (trimmed.Contains(':'))
            {
                return ParseScorm12Timespan(trimmed);
            }

            // Try ISO8601 duration (SCORM 2004): starts with 'P'
            if (trimmed.StartsWith('P') || trimmed.StartsWith('p'))
            {
                return ParseIso8601Duration(trimmed);
            }

            return 0;
        }

        /// <summary>
        /// Converts total seconds to a formatted duration string for the given SCORM version.
        /// "1.2" → "HHHH:MM:SS", other → "PTnHnMnS"
        /// </summary>
        public static string FromSeconds(int seconds, string scormVersion)
        {
            if (seconds <= 0)
            {
                return IsScorm12(scormVersion) ? "0000:00:00" : "PT0S";
            }

            if (IsScorm12(scormVersion))
            {
                int h = seconds / 3600;
                int m = (seconds % 3600) / 60;
                int s = seconds % 60;
                return $"{h:D4}:{m:D2}:{s:D2}";
            }
            else
            {
                int h = seconds / 3600;
                int m = (seconds % 3600) / 60;
                int s = seconds % 60;

                var parts = new System.Text.StringBuilder("PT");
                if (h > 0) parts.Append($"{h}H");
                if (m > 0) parts.Append($"{m}M");
                if (s > 0 || (h == 0 && m == 0)) parts.Append($"{s}S");
                return parts.ToString();
            }
        }

        private static int ParseScorm12Timespan(string value)
        {
            // Format: HHHH:MM:SS or HHHH:MM:SS.cc (centiseconds)
            var dotIndex = value.IndexOf('.');
            var mainPart = dotIndex >= 0 ? value[..dotIndex] : value;
            var fractionalSeconds = 0.0;

            if (dotIndex >= 0 && dotIndex < value.Length - 1)
            {
                if (double.TryParse("0." + value[(dotIndex + 1)..], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var frac))
                {
                    fractionalSeconds = frac;
                }
            }

            var parts = mainPart.Split(':');
            if (parts.Length != 3)
                return 0;

            if (!int.TryParse(parts[0], out var hours) ||
                !int.TryParse(parts[1], out var minutes) ||
                !int.TryParse(parts[2], out var secs))
            {
                return 0;
            }

            if (hours < 0 || minutes < 0 || minutes > 59 || secs < 0 || secs > 59)
                return 0;

            var totalSeconds = (hours * 3600) + (minutes * 60) + secs + fractionalSeconds;
            return (int)Math.Round(totalSeconds, MidpointRounding.AwayFromZero);
        }

        private static int ParseIso8601Duration(string value)
        {
            try
            {
                var ts = XmlConvert.ToTimeSpan(value);
                return (int)Math.Round(ts.TotalSeconds, MidpointRounding.AwayFromZero);
            }
            catch
            {
                return 0;
            }
        }

        private static bool IsScorm12(string? scormVersion)
        {
            return string.Equals(scormVersion?.Trim(), "1.2", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(scormVersion?.Trim(), ScormRuntimeFieldMap.Scorm12, StringComparison.OrdinalIgnoreCase);
        }
    }
}
