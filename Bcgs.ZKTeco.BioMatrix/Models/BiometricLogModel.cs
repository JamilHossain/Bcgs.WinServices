using System;

namespace Bcgs.ZKTeco.BioMatrix.Models
{
    public class BiometricLogModel
    {
        public int MachineNumber { get; set; }
        public int IndRegID { get; set; }
        public string DateTimeRecord1 { get; set; }

        public DateTime DateOnlyRecord
        {
            get { return DateTime.Parse(DateTime.Parse(DateTimeRecord1).ToString("yyyy-MM-dd")); }
        }
        public DateTime DateTimeRecord
        {
            get { return DateTime.Parse(DateTimeRecord1); }
        }

    }
}
