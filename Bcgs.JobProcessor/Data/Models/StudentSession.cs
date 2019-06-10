using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Bcgs.JobProcessor.Data.Models
{
    public class StudentSession
    {

        [Key]
        public int id { get; set; }
        //public int session_id { get; set; }
        public int student_id { get; set; }
        //public int class_id { get; set; }
        //public int section_id { get; set; }
        //public int route_id { get; set; }
        //public int hostel_room_id { get; set; }
        //public int vehroute_id { get; set; }
        //public int transport_fees { get; set; }
        //public int fees_discount { get; set; }
        public string is_active { get; set; }
        //public int created_at { get; set; }
        //public int updated_at { get; set; }

        public Student Student { get; set; }
        public ICollection<StudentAttendence> StudentAttendences { get; set; }

    }
}
