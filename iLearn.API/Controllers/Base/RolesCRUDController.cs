using DevExtreme.AspNet.Data;
using DevExtreme.AspNet.Mvc;
using iLearn.Application.DTOs;
using iLearn.Application.Interfaces.Repositories;
using iLearn.Application.Interfaces.Services;
using iLearn.Application.Services;
using iLearn.Domain.Entities;
using iLearn.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;

namespace iLearn.API.Controllers.Base
{
    [Authorize(Policy = "SuperAdminOnly")]
    public class RolesCRUDController : GenericController<Role>
    {
        public RolesCRUDController(
            IGenericRepository<Role> repository,
            ICurrentUserService currentUser) : base(repository, currentUser) { }
    }
}
