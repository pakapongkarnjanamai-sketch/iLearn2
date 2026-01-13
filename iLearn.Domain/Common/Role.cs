using iLearn.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iLearn.Domain.Common
{
    public class Role : BaseEntity
    {


        public int? DivisionId { get; set; }
        public Division? Division { get; set; }

        public RoleType? RoleType { get; set; }

        // Navigation property
        public ICollection<UserRole> UserRoles { get; set; }

        public Role()
        {
            UserRoles = new HashSet<UserRole>();
        }
    }
}
