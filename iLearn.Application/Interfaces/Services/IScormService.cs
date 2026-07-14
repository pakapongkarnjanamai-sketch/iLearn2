using iLearn.Application.DTOs;

namespace iLearn.Application.Interfaces.Services
{
    public interface IScormService
    {
        Task<ScormManifestDto> ExtractAndParseScormAsync(byte[] fileContent, string folderName);
        Task<ScormManifestDto> ExtractAndParseScormFromFileAsync(string zipFilePath, string folderName);
        Task<string> SavePackageToArchiveAsync(Stream stream, string archiveFileName);
        void DeleteScormFolder(string folderName);
        void DeleteArchiveFile(string storagePath);
        string GetScormUrl(string folderName, string launchHref);
        string GetArchiveFullPath(string relativePath);
        (int FileCount, long TotalSize) GetFolderInfo(string folderName);
    }
}