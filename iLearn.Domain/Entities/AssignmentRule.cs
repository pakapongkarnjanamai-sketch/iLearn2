using iLearn.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iLearn.Domain.Entities
{
    public class AssignmentRule : BaseEntity
    {
   
        public int CourseId { get; set; }
        [ForeignKey("CourseId")]
        public Course? Course { get; set; }

        // เงื่อนไข: ถ้าเป็น Null แปลว่า "ทั้งหมด"
        public int? DivisionId { get; set; }
        public Division? Division { get; set; }

        public int? RoleId { get; set; }
        public Role? Role { get; set; }
    }
}
