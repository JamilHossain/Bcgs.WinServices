
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using Bcgs.JobProcessor;

namespace Bcgs.WinService.Scheduler
{
    class Worker
    {
        private log4net.ILog logger;

        IAttendanceProcessor attendanceProcessor;
        public Worker(log4net.ILog logger)
        {
            this.logger = logger;
            this.attendanceProcessor = new AttendanceProcessor(logger);
        }

        private bool isworking = false;
        internal void Start()
        {
#if TRACE
            //logger.Info( "Worker.Start");
#endif
            logger.Info("Worker.Start");

            //Do Work
            UInt64 i = 0;
            isworking = true;
            while (isworking)
            {
                attendanceProcessor.ExecuteAttendanceJobAsync();               

                if (i == 3600) i = 0;
                i += 10;
                System.Threading.Thread.Sleep(1000*10);
            }
        }
       

        internal void Stop()
        {

            logger.Info("Worker.Stop");

            //Stop doing work
            isworking = false;
        }
    }
}
