using iLearn.Application.Interfaces.Repositories;
using iLearn.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace iLearn.API.Controllers.Base
{
    public class CategoriesCRUDController : GenericController<Category>
    {
        public CategoriesCRUDController(IGenericRepository<Category> repository) : base(repository)
        {
        }
    }

    public class AssignmentRulesCRUDController : GenericController<AssignmentRule>
    {
        public AssignmentRulesCRUDController(IGenericRepository<AssignmentRule> repository) : base(repository)
        {
        }
    }

    public class CoursesCRUDController : GenericController<Course>
    {
        public CoursesCRUDController(IGenericRepository<Course> repository) : base(repository)
        {
        }
    }

    public class DivisionsCRUDController : GenericController<Division>
    {
        public DivisionsCRUDController(IGenericRepository<Division> repository) : base(repository)
        {
        }
    }

    public class EnrollmentsCRUDController : GenericController<Enrollment>
    {
        public EnrollmentsCRUDController(IGenericRepository<Enrollment> repository) : base(repository)
        {
        }
    }

    public class FileStoragesCRUDController : GenericController<FileStorage>
    {
        public FileStoragesCRUDController(IGenericRepository<FileStorage> repository) : base(repository)
        {
        }
    }

    public class LearningLogsCRUDController : GenericController<LearningLog>
    {
        public LearningLogsCRUDController(IGenericRepository<LearningLog> repository) : base(repository)
        {
        }
    }

    public class ResourcesCRUDController : GenericController<Resource>
    {
        public ResourcesCRUDController(IGenericRepository<Resource> repository) : base(repository)
        {
        }
    }

    public class RolesCRUDController : GenericController<Role>
    {
        public RolesCRUDController(IGenericRepository<Role> repository) : base(repository)
        {
        }
    }

    public class UsersCRUDController : GenericController<User>
    {
        public UsersCRUDController(IGenericRepository<User> repository) : base(repository)
        {
        }
    }

    public class UserRolesCRUDController : GenericController<UserRole>
    {
        public UserRolesCRUDController(IGenericRepository<UserRole> repository) : base(repository)
        {
        }
    }

}
