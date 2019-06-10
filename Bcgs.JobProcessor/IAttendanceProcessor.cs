using System;
using System.Collections.Generic;
using System.Text;

namespace Bcgs.JobProcessor
{
    public interface IAttendanceProcessor
    {
        void ExecuteAttendanceJobAsync();
    }
}
