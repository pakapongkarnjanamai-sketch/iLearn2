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

        public StudentGroupsController(IStudentGroupService service)
        {
            _service = service;
        }

        // GET: api/studentgroups
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _service.GetAllAsync();
            return Ok(new { success = true, data = result, totalCount = result.Count });
        }

        // GET: api/studentgroups/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _service.GetByIdAsync(id);
            if (result == null) return NotFound(new { message = $"?????????????????? id={id}" });
            return Ok(new { success = true, data = result });
        }

        // POST: api/studentgroups
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateStudentGroupDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "??????????????????" });

            var result = await _service.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new { success = true, data = result });
        }

        // PUT: api/studentgroups/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateStudentGroupDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { message = "??????????????????" });

            try
            {
                await _service.UpdateAsync(id, dto);
                return Ok(new { success = true, message = "?????????????????????????" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"?????????????????? id={id}" });
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
                return NotFound(new { message = $"?????????????????? id={id}" });
            }
        }

        // POST: api/studentgroups/5/members
        [HttpPost("{id}/members")]
        public async Task<IActionResult> AddMembers(int id, [FromBody] AddGroupMembersDto dto)
        {
            if (dto.StudentCodes == null || dto.StudentCodes.Count == 0)
                return BadRequest(new { message = "?????????????????????????????? 1 ??????" });

            try
            {
                await _service.AddMembersAsync(id, dto);
                return Ok(new { success = true, message = "??????????????????????????" });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"?????????????????? id={id}" });
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
                return NotFound(new { message = "???????????????????????" });
            }
        }

        // GET: api/studentgroups/5/student-codes
        // ?????????? BulkAssign — ??? StudentCodes ??????????? AssignmentsController
        [HttpGet("{id}/student-codes")]
        public async Task<IActionResult> GetStudentCodes(int id)
        {
            try
            {
                var codes = await _service.GetStudentCodesAsync(id);
                return Ok(new { success = true, data = codes });
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = $"?????????????????? id={id}" });
            }
        }
    }
}
