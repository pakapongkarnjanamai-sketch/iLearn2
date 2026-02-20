

using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentsController : ControllerBase
    {
        private readonly IStudentApiService _studentService;
        public StudentsController(IStudentApiService studentService) {
            _studentService = studentService;
        }

        [HttpGet("GetStudentbyEID/{employeeCode}")]
        public async Task<ExternalStudentDto> GetStudentbyEID(string employeeCode)
        {
            var student = await _studentService.GetStudentByCodeAsync(employeeCode);

            return student;
        }
        [HttpGet("GetStudentAsync")]
        public async Task<StudentDto> GetStudentAsync()
        {
            var student = await _studentService.GetStudentAsync();

            return student;
        }
    }
}
