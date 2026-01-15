using iLearn.API.Controllers.Base;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Mappings;
using iLearn.Application.Services;
using iLearn.Domain.Common;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IGenericRepository<User> _userRepo;
        private readonly ICourseAssignmentService _assignmentService;
        public readonly IDateTime _dateTime;
        private readonly ILogger<UsersController> _logger;
        public UsersController(
            IGenericRepository<User> userRepo,
            ICourseAssignmentService assignmentService,IDateTime dateTime, ILogger<UsersController> logger)
        {
            _dateTime = dateTime;
            _userRepo = userRepo;
            _assignmentService = assignmentService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userRepo.GetAllAsync();
            var dtos = users.Select(u => u.ToDto());
            return Ok(dtos);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound();
            return Ok(user.ToDto());
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        {
            // 1. สร้าง User ตามปกติ
            var user = dto.ToEntity();

            // (ควรเช็ค User ซ้ำตรงนี้ด้วย Nid)

            var createdUser = await _userRepo.AddAsync(user);

            // 2. [สำคัญ] Trigger ระบบ Auto-Assignment! 🚀
            // ระบบจะไปค้นหาคอร์ส General ทั้งหมดแล้วยัดให้ User คนนี้ทันที
            await _assignmentService.AssignGeneralCoursesToNewUserAsync(createdUser.Id);

            return CreatedAtAction(nameof(GetById), new { id = createdUser.Id }, createdUser.ToDto());
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null) return NotFound();

            await _userRepo.DeleteAsync(user);
            return NoContent();
        }

        [HttpPost("windows-auth")]
        public async Task<ActionResult<ApiResponse<UserDto>>> GetOrCreateUserFromWindows([FromBody] CreateUserRequest request)
        {
            try
            {
                // 1. Validate input และ extract NID
                if (string.IsNullOrWhiteSpace(request?.WindowsIdentity))
                {
                    return BadRequest(new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "Windows identity is required"
                    });
                }

                string nid = request.WindowsIdentity.Split('\\').LastOrDefault();
                if (string.IsNullOrWhiteSpace(nid))
                {
                    return BadRequest(new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "Invalid Windows identity format"
                    });
                }

                // 2. Query optimization - เลือกเฉพาะ field ที่ต้องการ
                var user = await _userRepo.GetQuery()
                    .Where(u => u.Nid == nid)
                    .Select(u => new
                    {
                        u.Id,
                     
                        u.LastLogin,
                        Roles = u.UserRoles.Select(ur => new RoleDto
                        {
                            Id = ur.Role.Id,
                            Name = ur.Role.Name,
                            //Description = ur.Role.Description,
                            //IsActive = ur.Role.IsActive
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return NotFound(new ApiResponse<UserDto>
                    {
                        Success = false,
                        Message = "User not found"
                    });
                }

                // 3. Update LastLogin แยกต่างหาก (เพื่อ performance)
                await _userRepo.GetQuery()
                    .Where(u => u.Nid == nid)
                    .ExecuteUpdateAsync(u => u.SetProperty(x => x.LastLogin, _dateTime.Now));

                // 4. Create UserDto โดยใช้ AutoMapper หรือ direct mapping
                var userDto = new UserDto
                {
                    Id = user.Id,
                    //NID = user.NID,
                    //EmployeeID = user.EmployeeID,
                    //FirstName = user.FirstName,
                    //LastName = user.LastName,
                    //Email = user.Email,
                    //PhoneNumber = user.PhoneNumber,
                    LastLogin = _dateTime.Now,
                    Roles = user.Roles
                };

                return Ok(new ApiResponse<UserDto>
                {
                    Success = true,
                    Data = userDto,
                    Message = "User retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetOrCreateUserFromWindows for identity: {Identity}",
                    request?.WindowsIdentity);

                return StatusCode(500, new ApiResponse<UserDto>
                {
                    Success = false,
                    Message = "Internal server error"
                });
            }
        }
    }
}