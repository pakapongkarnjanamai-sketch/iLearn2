using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iLearn.Domain.Common
{
    public class User : BaseEntity
    {

        public string Nid { get; set; }

        // Navigation property
        public ICollection<UserRole> UserRoles { get; set; }
        public User() { UserRoles = new HashSet<UserRole>(); }
    }
}
