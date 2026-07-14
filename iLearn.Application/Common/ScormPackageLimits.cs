namespace iLearn.Application.Common
{
    public static class ScormPackageLimits
    {
        public const long MaxCompressedPackageBytes = 200L * 1024 * 1024;              // 200 MB (ZIP)
        public const long MaxRequestEnvelopeBytes = MaxCompressedPackageBytes + (10L * 1024 * 1024);  // 210 MB (auto)
        public const int MaxArchiveEntries = 1000;
        public const long MaxSingleEntryUncompressedBytes = 200L * 1024 * 1024;        // 200 MB
        public const long MaxTotalUncompressedBytes = 500L * 1024 * 1024;              // 500 MB
    }
}