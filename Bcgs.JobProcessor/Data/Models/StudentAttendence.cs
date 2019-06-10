
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Bcgs.JobProcessor.Data.Models
{
    //[Table("pessoa")]
    public class StudentAttendence
    {
        [Key]
        public int id { get; set; }
        public int student_session_id { get; set; }
        public DateTime date { get; set; }
        public int attendence_type_id { get; set; }
        public string remark { get; set; }
        public string is_active { get; set; }
        public DateTime created_at { get; set; }
        public DateTime updated_at { get; set; }

        public StudentSession StudentSession { get; set; }

    }
}
