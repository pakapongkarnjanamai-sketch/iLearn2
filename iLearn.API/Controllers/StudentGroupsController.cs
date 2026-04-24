using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace iLearn.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentGroupsController : ControllerBase
    {
        private readonly IStudentGroupService _service;
        private readonly ICurrentUserService _currentUser;
        public StudentGroupsController(IStudentGroupService service, ICurrentUserService currentUser)
        {
            _service = service;
            _currentUser = currentUser;
        }

        // GET: api/studentgroups
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            // ✅ Data Isolation จัดการใน Service layer แล้ว
            var result = await _service.GetAllAsync();
            return Ok(new { success = true, data = result, totalCount = result.Count });
        }

        // GET: api/studentgroups/paged?page=1&pageSize=20&search=...&sortBy=name&sortDescending=false
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged([FromQuery] PaginationParams p)
        {
            var result = await _service.GetPagedAsync(p);
            return Ok(new { success = true, data = result.Data, totalCount = result.TotalCount });
        }

        // GET: api/studentgroups/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound(new { message = $"Student group with ID {id} was not found." });
            return Ok(new { success = true, data = result });
        }

        // POST: api/studentgroups
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStudentGroupDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "Group name is required." });

            if (string.IsNullOrWhiteSpace(dto.Description))
                return BadRequest(new { message = "Description is required." });

            try
            {
                var result = await _service.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = result.Id },
                    new { success = true, data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT: api/studentgroups/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateStudentGroupDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "Group name is required." });

            if (string.IsNullOrWhiteSpace(dto.Description))
                return BadRequest(new { message = "Description is required." });

            try
            {
                await _service.UpdateAsync(id, dto);
                return Ok(new { success = true, message = "Student group updated successfully." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Student group with ID {id} was not found." });
            }
        }

        // DELETE: api/studentgroups/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _service.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Student group with ID {id} was not found." });
            }
        }

        // POST: api/studentgroups/5/members
        [HttpPost("{id}/members")]
        public async Task<IActionResult> AddMembers(int id, [FromBody] AddGroupMembersDto dto)
        {
            if (dto.StudentCodes == null || dto.StudentCodes.Count == 0)
                return BadRequest(new { message = "At least one employee code is required." });

            try
            {
                await _service.AddMembersAsync(id, dto);
                return Ok(new { success = true, message = "Members added successfully." });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Student group with ID {id} was not found." });
            }
        }

        [HttpPost("{id}/members/preview")]
        public async Task<IActionResult> PreviewAddMembers(int id, [FromBody] StudentGroupAddMembersOptionsDto dto)
        {
            try
            {
                var result = await _service.PreviewAddMembersAsync(id, dto);
                return Ok(new { success = true, data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Student group with ID {id} was not found." });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost("{id}/members/confirm")]
        public async Task<IActionResult> ConfirmAddMembers(int id, [FromBody] StudentGroupAddMembersOptionsDto dto)
        {
            try
            {
                var result = await _service.AddMembersWithAssignmentsAsync(id, dto);
                return Ok(new { success = true, data = result, message = "Members updated successfully." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Student group with ID {id} was not found." });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        // DELETE: api/studentgroups/5/members/10
        [HttpDelete("{id}/members/{memberId}")]
        public async Task<IActionResult> RemoveMember(int id, int memberId)
        {
            try
            {
                await _service.RemoveMemberAsync(id, memberId);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "Member not found in the group." });
            }
        }

        [HttpPost("{id}/members/remove/preview")]
        public async Task<IActionResult> PreviewRemoveMembers(int id, [FromBody] StudentGroupRemoveMembersOptionsDto dto)
        {
            try
            {
                var result = await _service.PreviewRemoveMembersAsync(id, dto);
                return Ok(new { success = true, data = result });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Student group with ID {id} was not found." });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        [HttpPost("{id}/members/remove/confirm")]
        public async Task<IActionResult> ConfirmRemoveMembers(int id, [FromBody] StudentGroupRemoveMembersOptionsDto dto)
        {
            try
            {
                var result = await _service.RemoveMembersWithAssignmentsAsync(id, dto);
                return Ok(new { success = true, data = result, message = "Members removed successfully." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"Student group with ID {id} was not found." });
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
    }
}
