using iLearn.Application.DTOs;
using System.Threading.Tasks;
using DevExtreme.AspNet.Mvc;
namespace iLearn.Application.Interfaces.Services
{
    public interface IStudentApiService
    {
        Task<ExternalStudentDto> GetStudentByCodeAsync(string Code);
        Task<AllStudentsApiResponse> GetStudentAsync();
        Task<DivisionApiResponse> GetStudentsByDivisionsAsync(string[] divisions, int skip = 0, int take = 20);

        // --- เพิ่มฟังก์ชันนี้เข้าไปใหม่ ---
        // ฟังก์ชันนี้จะทำหน้าที่เป็น Proxy รับ Query String ส่งไปให้ API ต้นทาง
        Task<string> GetStudentsDxGridAsync(string queryString);

        Task<object> GetSectionsAsync(string queryString);
        Task<object> GetDivisionsAsync(string queryString);
        Task<object> GetDepartmentsAsync(string queryString);
    }
}