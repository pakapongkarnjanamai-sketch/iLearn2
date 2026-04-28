namespace iLearn.Application.Common
{
    public static class ScormRuntimeLimits
    {
        public const int ScormVersionMaxLength = 32;
        public const int LessonLocationMaxLength = 2048;
        public const int StatusMaxLength = 64;
        public const int SessionTimeMaxLength = 64;
        public const int TotalTimeMaxLength = 64;
        public const int EntryMaxLength = 64;
        public const int ExitMaxLength = 64;

        // Runtime payload hardening limits. These are transport guards, not DB schema limits.
        public const int SuspendDataMaxLength = 65535;
        public const int CmiSnapshotJsonMaxLength = 262144;
    }
}