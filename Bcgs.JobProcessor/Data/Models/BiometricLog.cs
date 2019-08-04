using System;
using System.Collections.Generic;
using System.Text;

namespace Bcgs.JobProcessor.Data.Models
{
    public class BiometricLog
    {
        public int id { get; set; }
        public int machine_id { get; set; }
        public int ind_reg_iD { get; set; }
        public DateTime datetime_record { get; set; }
        public bool is_processed { get; set; } = false;
    }
}
