using iLearn.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iLearn.Domain.Entities
{
    public class User : BaseEntity
    {

        public string Nid { get; set; }
        public DateTime LastLogin { get; set; }
        // Navigation property
        public ICollection<UserRole> UserRoles { get; set; }
        public User() { UserRoles = new HashSet<UserRole>(); }
    }
}
