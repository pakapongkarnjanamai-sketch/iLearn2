using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Mappings;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using System.Linq; // จำเป็นสำหรับการใช้ .Where และ .Select
using System.Threading.Tasks;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DivisionsController : ControllerBase
    {
        private readonly IGenericRepository<Division> _divisionRepo;
        private readonly IGenericRepository<Category> _categoryRepo; // เพิ่ม Repo ของ Category

        // Inject Category Repository เข้ามาผ่าน Constructor
        public DivisionsController(
            IGenericRepository<Division> divisionRepo,
            IGenericRepository<Category> categoryRepo)
        {
            _divisionRepo = divisionRepo;
            _categoryRepo = categoryRepo;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _divisionRepo.GetAllAsync();
            return Ok(items.Select(x => x.ToDto()));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DivisionDto dto)
        {
            var entity = new Division { Name = dto.Name };
            var result = await _divisionRepo.AddAsync(entity);
            return Ok(result.ToDto());
        }

        // --- API GetTree สำหรับ TreeView ---
        [HttpGet("GetTree")]
        public async Task<IActionResult> GetTree()
        {
            var divisions = await _divisionRepo.GetAllAsync();
            var categories = await _categoryRepo.GetAllAsync();

            // ไม่ต้องกรอง IsActive ออกแล้ว แต่ใช้การเช็คเพื่อแสดงสัญลักษณ์แทน
            var divisionNodes = divisions
                .OrderBy(d => d.Name)
                .Select(d => new
                {
                    id = "div_" + d.Id,
                    // ถ้า IsActive เป็น false ให้เติม [Draft] หรือสัญลักษณ์ที่คุณต้องการ
                    text = d.IsActive ? d.Name : $"🚫 {d.Name} (Draft)",
                    icon = "folder",
                    expanded = true,
                    isDivision = true,
                    divisionId = d.Id,
                    items = categories
                        .Where(c => c.DivisionId == d.Id) // เอาเงื่อนไข IsActive ออกเช่นกัน
                        .OrderBy(c => c.Name)
                        .Select(c => new
                        {
                            id = "cat_" + c.Id,
                            text = c.IsActive ? c.Name : $"🚫 {c.Name} (Draft)",
                            icon = "file",
                            categoryId = c.Id
                        }).ToList()
                }).ToList();

            var rootNode = new[]
            {
                new
                {
                    id = "root",
                    text = "All Courses",
                    icon = "home",
                    expanded = true,
                    isRoot = true,
                    items = divisionNodes
                }
            };

            return Ok(rootNode);
        }
    }
}