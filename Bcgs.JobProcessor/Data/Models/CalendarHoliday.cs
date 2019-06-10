using System;
using System.Collections.Generic;
using System.Text;

namespace Bcgs.JobProcessor.Data.Models
{
    public class CalendarHoliday
    {
        public int id { get; set; }
        public DateTime date { get; set; }
        public bool is_holiday { get; set; }
        public bool is_weekend { get; set; }
        public string description { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
    }
}
