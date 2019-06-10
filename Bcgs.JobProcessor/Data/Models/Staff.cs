using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Bcgs.JobProcessor.Data.Models
{

    public class Staff
    {
        [Key]
        public int id { get; set; }
        public string employee_id { get; set; }
        public string name { get; set; }
        public string surname { get; set; }
        public string contact_no { get; set; }
        public string email { get; set; }
        public int is_active { get; set; }

        [NotMapped]
        public bool IsActive { get { return is_active == 0 ? false : true; } }
    }
}
