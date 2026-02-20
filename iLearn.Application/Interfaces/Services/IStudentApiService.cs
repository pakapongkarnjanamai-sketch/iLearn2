using iLearn.Application.DTOs;
using System.Threading.Tasks;

namespace iLearn.Application.Interfaces.Services
{
    public interface IStudentApiService
    {
        // ดึงข้อมูลนักเรียนจากรหัส
        Task<ExternalStudentDto> GetStudentByCodeAsync(string Code);

        // ดึงข้อมูลนักเรียนทั้งหมด (หรือตามที่ API กำหนด)
        Task<StudentDto> GetStudentAsync();

        // ฟังก์ชันใหม่: ดึงข้อมูลนักเรียนตามแผนก (Divisions) พร้อมรองรับการแบ่งหน้า (Pagination)
        Task<DivisionApiResponse> GetStudentsByDivisionsAsync(string[] divisions, int skip = 0, int take = 20);
    }
}