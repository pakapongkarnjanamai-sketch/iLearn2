using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
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
        private readonly ICurrentUserService _currentUser;

        // Inject Category Repository เข้ามาผ่าน Constructor
        public DivisionsController(
            IGenericRepository<Division> divisionRepo,
            IGenericRepository<Category> categoryRepo,
            ICurrentUserService currentUser)
        {
            _divisionRepo = divisionRepo;
            _categoryRepo = categoryRepo;
            _currentUser = currentUser;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _divisionRepo.GetAllAsync();

            // ── Data Isolation: ถ้ามี DivisionId ให้กรองเฉพาะ Division ของตัวเอง ──
            if (_currentUser.DivisionId.HasValue)
            {
                items = items.Where(d => d.Id == _currentUser.DivisionId.Value).ToList();
            }

            return Ok(items.Select(x => x.ToDto()));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DivisionDto dto)
        {
            var entity = new Division { Name = dto.Name };
            var result = await _divisionRepo.AddAsync(entity);
            return Ok(result.ToDto());
        }

        /// <summary>
        /// Resolve Division Name -> DivisionId สำหรับ iLearn.User ตอน Login
        /// </summary>
        [HttpGet("resolve-id")]
        public async Task<IActionResult> ResolveId([FromQuery] string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return BadRequest(new { message = "Division name is required" });

            var divisions = await _divisionRepo.GetAsync(
                filter: d => d.Name == name
            );
            var division = divisions.FirstOrDefault();

            if (division == null)
                return NotFound(new { divisionId = 0, message = $"Division '{name}' not found" });

            return Ok(new { divisionId = division.Id });
        }

        // --- API GetTree สำหรับ TreeView ---
        [HttpGet("GetTree")]
        public async Task<IActionResult> GetTree()
        {
            var divisions = await _divisionRepo.GetAllAsync();
            var categories = await _categoryRepo.GetAllAsync();

            // ── Data Isolation: ถ้ามี DivisionId ให้กรองเฉพาะ Division ของตัวเอง ──
            if (_currentUser.DivisionId.HasValue)
            {
                var myDivId = _currentUser.DivisionId.Value;
                divisions = divisions.Where(d => d.Id == myDivId).ToList();
                categories = categories.Where(c => c.DivisionId == myDivId).ToList();
            }

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