using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;

using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace iLearn.API.Controllers.Base
{
    [ApiController]
    [Route("api/admin/[controller]")]
    [Authorize]
    public class GenericController<T> : ControllerBase where T : BaseEntity, new()
    {
        private const int MaxTakePerRequest = 200;

        protected readonly IGenericRepository<T> _repository;
        protected readonly ICurrentUserService _currentUser;

        public GenericController(IGenericRepository<T> repository, ICurrentUserService currentUser)
        {
            _repository  = repository;
            _currentUser = currentUser;
        }

        /// <summary>
        /// Caps the Take value in DataSourceLoadOptions to prevent excessively large data fetches.
        /// </summary>
        protected static void CapLoadOptionsTake(DataSourceLoadOptions loadOptions, int maxTake = MaxTakePerRequest)
        {
            if (loadOptions.Take <= 0 || loadOptions.Take > maxTake)
                loadOptions.Take = maxTake;
        }

        [HttpGet("Get")]
        public virtual async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {
            CapLoadOptionsTake(loadOptions);
            var data = await _repository.GetAllAsync();
            return Ok(DataSourceLoader.Load(data, loadOptions));
        }

        [HttpGet("Get/{id}")]
        public virtual async Task<IActionResult> Get(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return NotFound();
            return Ok(entity);
        }

        [HttpPost("Post")]
        public virtual async Task<IActionResult> Post([FromForm] string values)
        {
            var newEntity = new T(); // บรรทัดนี้ต้องการ Constraint new()
            JsonConvert.PopulateObject(values, newEntity);

            if (!TryValidateModel(newEntity))
                return BadRequest(ModelState);

            await _repository.AddAsync(newEntity);
            return Ok(newEntity);
        }

        [HttpPut("Put")]
        public virtual async Task<IActionResult> Put([FromForm] int key, [FromForm] string values)
        {
            var entity = await _repository.GetByIdAsync(key);
            if (entity == null) return NotFound();

            JsonConvert.PopulateObject(values, entity);

            if (!TryValidateModel(entity))
                return BadRequest(ModelState);

            await _repository.UpdateAsync(entity);
            return Ok(entity);
        }

        [HttpDelete("Delete")]
        public virtual async Task<IActionResult> Delete([FromForm] int key)
        {
            var entity = await _repository.GetByIdAsync(key);
            if (entity == null) return NotFound();

            await _repository.DeleteAsync(entity);
            return Ok();
        }
    }
}