namespace iLearn.Application.Common
{
    public static class ScormPackageLimits
    {
        public const long MaxCompressedPackageBytes = 1024L * 1024 * 1024;             // 1 GB (ZIP)
        public const long MaxRequestEnvelopeBytes = MaxCompressedPackageBytes + (10L * 1024 * 1024);  // 1034 MB (auto)
        public const int MaxArchiveEntries = 1000;
        public const long MaxSingleEntryUncompressedBytes = 1024L * 1024 * 1024;       // 1 GB (single video)
        public const long MaxTotalUncompressedBytes = 2560L * 1024 * 1024;             // 2.5 GB (zip-bomb guard)
    }
}