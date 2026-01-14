namespace iLearn.Application.Interfaces.Services
{
    public interface IScormService
    {
        // รับเนื้อหาไฟล์ (byte[]) และชื่อโฟลเดอร์ปลายทาง (เช่น ResourceId)
        // คืนค่าเป็น URL เริ่มต้น (Launch URL) ของ SCORM
        Task<string> ExtractAndParseScormAsync(byte[] fileContent, string folderName);
        void DeleteScormFolder(string folderName);
    }
}