namespace iLearn.Application.Common
{
    public static class ScormPackageLimits
    {
        public const long MaxCompressedPackageBytes = 100L * 1024 * 1024;
        public const long MaxRequestEnvelopeBytes = MaxCompressedPackageBytes + (10L * 1024 * 1024);
        public const int MaxArchiveEntries = 1000;
        public const long MaxSingleEntryUncompressedBytes = 100L * 1024 * 1024;
        public const long MaxTotalUncompressedBytes = 250L * 1024 * 1024;
    }
}