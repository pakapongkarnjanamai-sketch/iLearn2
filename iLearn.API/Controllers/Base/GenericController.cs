using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Domain.Common; // ใช้ BaseEntity ของเรา
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace iLearn.API.Controllers.Base
{
    [Route("api/[controller]")]
    [ApiController]
    public abstract class GenericController<T> : ControllerBase where T : BaseEntity, new()
    {
        protected readonly IGenericRepository<T> _repository;
        // ถ้าต้องการ Logger ให้ Uncomment
        // protected readonly ILogger<GenericController<T>> _logger;

        public GenericController(IGenericRepository<T> repository)
        {
            _repository = repository;
        }

        // GET: api/[controller]
        // รองรับการ Filter, Sort, Group, Paging จาก DevExtreme
        [HttpGet]
        public virtual async Task<IActionResult> Get(DataSourceLoadOptions loadOptions)
        {
            // หมายเหตุ: GenericRepository ปัจจุบันคืนค่าเป็น List (Fetch All)
            // ทำให้การ Filter เกิดขึ้นใน Memory (Server-side evaluation)
            // ถ้าข้อมูลเยอะมาก ควรปรับ Repository ให้คืนค่า IQueryable
            var data = await _repository.GetAllAsync();

            return Ok(DataSourceLoader.Load(data, loadOptions));
        }

        // GET: api/[controller]/5
        [HttpGet("{id}")]
        public virtual async Task<IActionResult> Get(int id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return NotFound();
            return Ok(entity);
        }

        // POST: api/[controller]
        [HttpPost]
        public virtual async Task<IActionResult> Post([FromForm] string values)
        {
            var model = new T(); // สร้าง Instance ใหม่
            JsonConvert.PopulateObject(values, model); // เอาค่าจาก Form ใส่ Model

            if (!TryValidateModel(model))
                return BadRequest(ModelState);

            await _repository.AddAsync(model);

            // GenericRepository ปกติจะ SaveChanges ให้เลย หรือต้องเรียกเองตาม UnitOfWork
            // ในที่นี้สมมติว่า AddAsync save ให้แล้ว หรือถ้าใช้ UoW ก็ต้องเรียก Save

            return Ok(model);
        }

        // PUT: api/[controller]/5
        [HttpPut]
        public virtual async Task<IActionResult> Put([FromForm] int key, [FromForm] string values)
        {
            var model = await _repository.GetByIdAsync(key);
            if (model == null) return NotFound();

            JsonConvert.PopulateObject(values, model);

            if (!TryValidateModel(model))
                return BadRequest(ModelState);

            await _repository.UpdateAsync(model);

            return Ok(model);
        }

        // DELETE: api/[controller]/5
        [HttpDelete]
        public virtual async Task<IActionResult> Delete([FromForm] int key)
        {
            var model = await _repository.GetByIdAsync(key);
            if (model == null) return NotFound();

            await _repository.DeleteAsync(model);

            return NoContent();
        }
    }
}