using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Bcgs.JobProcessor.Data.Models
{
    public class AttendanceJobConfig
    {
        [Key]
        public int id { get; set; }
        public TimeSpan shift1_late_attendance_cutoff_time { get; set; }
        public TimeSpan shift2_late_attendance_cutoff_time { get; set; }
        public TimeSpan shift1_absent_notification_cutoff_time { get; set; }
        public TimeSpan shift2_absent_notification_cutoff_time { get; set; }
        public TimeSpan staff_late_attendance_cutoff_time { get; set; }
        public TimeSpan job_start_time { get; set; }
        public TimeSpan job_end_time { get; set; }
        public int interval_minute { get; set; }
        public string present_sms { get; set; }
        public string late_sms { get; set; }
        public string absent_sms { get; set; }
        public bool is_enable_sms_service { get; set; }
        public bool is_active { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? updated_at { get; set; }
    }
}
