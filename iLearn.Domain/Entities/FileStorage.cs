using iLearn.Domain.Common;

namespace iLearn.Domain.Entities
{
    public class FileStorage : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;

        public byte[]? Data { get; set; }
        public long Length { get; set; }
    }
}