using iLearn.Domain.Common;

namespace iLearn.Domain.Entities
{
    public class FileStorage : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;

        public byte[]? Data { get; set; }
        public long Length { get; set; }

        /// <summary>
        /// Relative path from FileSettings.HostUnc to the stored ZIP archive on disk.
        /// When set, Data is null — the file lives on the file share instead of in the DB.
        /// </summary>
        public string? StoragePath { get; set; }
    }
}