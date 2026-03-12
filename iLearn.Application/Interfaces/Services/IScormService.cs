using iLearn.Application.DTOs;

namespace iLearn.Application.Interfaces.Services
{
    public interface IScormService
    {
        Task<ScormManifestDto> ExtractAndParseScormAsync(byte[] fileContent, string folderName);
        void DeleteScormFolder(string folderName);
        string GetScormUrl(string folderName,string resourceHref);
        (int FileCount, long TotalSize) GetFolderInfo(string folderName);
    }
}