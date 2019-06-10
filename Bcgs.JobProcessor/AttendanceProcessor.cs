using Bcgs.JobProcessor.Data;
using Bcgs.ZKTeco.BioMatrix;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Linq;
using Bcgs.ZKTeco.BioMatrix.Models;
using BasecampSMSLib;
using Bcgs.JobProcessor.Data.Models;
using Bcgs.JobProcessor.Enums;

namespace Bcgs.JobProcessor
{
    public class AttendanceProcessor : IAttendanceProcessor
    {
        private readonly log4net.ILog logger;
        private Data.Models.AttendanceJobConfig AttendanceConfig;
        private DateTime DailyProcess_LastRunTime { get; set; } = DateTime.MinValue;
        private DateTime ProcessAttendanceLog_LastRunTime { get; set; } = DateTime.MinValue;
        private DateTime SendAbsentSms_LastRunTime { get; set; } = DateTime.MinValue;

        private bool IsBusy { get; set; } = false;

        private const int StudentAbsentTypeId = 4;
        private const int StudentPresentTypeId = 1;
        private const int StudentLatePresentTypeId = 3;
        private const int StudentHolidayTypeId = 5;


        private const int StaffAbsentTypeId = 3;
        private const int StaffPresentTypeId = 1;
        private const int StaffLatePresentTypeId = 2;
        private const int StaffHolidayTypeId = 5;


        public AttendanceProcessor(log4net.ILog logger)
        {
            this.logger = logger;
            LoadAttendanceConfig();
        }

        public async void ExecuteAttendanceJobAsync()
        {
            this.InitializeDailyAttendanceProcess();
            this.ProcessDailyAttendanceLog();
            await this.SendDailyAbsentSms();
        }

        private void InitializeDailyAttendanceProcess()
        {


            if (!(DateTime.Now.Hour == 0
                && this.DailyProcess_LastRunTime.Date < DateTime.Now.Date
                 && !IsBusy))
            {
                return;
            }


            this.IsBusy = true;
            logger.Info($"InitiateDailyAttendanceProcessAsync - Start Time: {DateTime.Now.ToString("MM/dd/yyyy h:mm:ss tt")}");


            this.DailyProcess_LastRunTime = DateTime.Now;
            try
            {
                using (AttendanceDbContext dbContext = new AttendanceDbContext())
                {
                    this.AttendanceConfig = dbContext.AttendanceJobConfigs.FirstOrDefault();

                    MakeStaffAbsent(dbContext);
                    MakeStudentAbsent(dbContext);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }
            finally
            {
                this.IsBusy = false;
                logger.Info($"InitiateDailyAttendanceProcessAsync - End Time: {DateTime.Now.ToString("MM/dd/yyyy h:mm:ss tt")}");
            }

        }
        
        private void ProcessDailyAttendanceLog()
        {

            bool isValidTimeRange = ((DateTime.Now.TimeOfDay > this.AttendanceConfig.job_start_time
                   && DateTime.Now.TimeOfDay < this.AttendanceConfig.job_end_time)
                       || DateTime.Now.TimeOfDay > (new TimeSpan(23, 54, 00)));

            if (!(isValidTimeRange
               && this.ProcessAttendanceLog_LastRunTime.AddMinutes(this.AttendanceConfig.interval_minute) < DateTime.Now
               && !this.IsBusy))
            {
                return;
            }

            logger.Info($"ProcessAttendanceLogAsync - Start Time: {DateTime.Now.ToString("MM/dd/yyyy h:mm:ss tt")}");

            this.IsBusy = true;


            this.ProcessAttendanceLog_LastRunTime = DateTime.Now;
            DateTime lastProcessDate = DateTime.MinValue;
            try
            {

                ICollection<Bcgs.ZKTeco.BioMatrix.Models.BioMatrixLog> biomatrixLogData;
                using (ZkTecoClient bioMatrixClient = new ZkTecoClient("basecampzkteco.ddns.net"))
                {
                    biomatrixLogData = bioMatrixClient.GetBioMatrixData();
                }


                //ICollection<Bcgs.ZKTeco.BioMatrix.Models.BioMatrixLog> biomatrixLogData = new HashSet<Bcgs.ZKTeco.BioMatrix.Models.BioMatrixLog>();
                //biomatrixLogData.Add(new ZKTeco.BioMatrix.Models.BioMatrixLog
                //{
                //    MachineNumber = 1,
                //    IndRegID = 19040001,
                //    DateTimeRecord1 = DateTime.Now.ToString()
                //});
                //biomatrixLogData.Add(new ZKTeco.BioMatrix.Models.BioMatrixLog
                //{
                //    MachineNumber = 1,
                //    IndRegID = 18010002,
                //    DateTimeRecord1 = DateTime.Now.ToString()
                //});


                using (AttendanceDbContext dbContext = new AttendanceDbContext())
                {
                    if (dbContext.BioMatrixLogs.Any())
                    {
                        lastProcessDate = dbContext.BioMatrixLogs.Max(x => x.datetime_record);
                    }

                    if (this.DailyProcess_LastRunTime.Date < DateTime.Now.Date)
                    {
                        this.MakeStudentAbsent(dbContext);
                        this.MakeStaffAbsent(dbContext);
                    }


                    biomatrixLogData = biomatrixLogData.Where(x => x.DateTimeRecord > lastProcessDate).ToList();


                    foreach (var log in biomatrixLogData)
                    {
                        try
                        {


                            dbContext.BioMatrixLogs.Add(new Data.Models.BioMatrixLog
                            {
                                machine_id = log.MachineNumber,
                                ind_reg_iD = log.IndRegID,
                                datetime_record = log.DateTimeRecord
                            });
                            ///Save BioMatrixLogs
                            dbContext.SaveChanges();


                            bool isStudentLog = this.UpdateStudentAttendanceStatus(log, dbContext);
                            if (!isStudentLog)
                            {
                                this.UpdateStaffAttendanceStatus(log, dbContext);
                            }

                        }
                        catch (Exception ex)
                        {
                            logger.Info(ex.Message, ex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }
            finally
            {
                this.IsBusy = false;
                logger.Info($"ProcessAttendanceLogAsync - End Time: {DateTime.Now.ToString("MM/dd/yyyy h:mm:ss tt")}");

            }
        }

        private async Task SendDailyAbsentSms()
        {
            if (!(this.SendAbsentSms_LastRunTime.Date < DateTime.Now.Date
                && DateTime.Now.TimeOfDay > this.AttendanceConfig.shift1_absent_notification_cutoff_time
                && !IsBusy))
            {
                return;
            }

            logger.Info($"SendAbsentSms - Start Time: {DateTime.Now.ToString("MM/dd/yyyy h:mm:ss tt")}");
            this.SendAbsentSms_LastRunTime = DateTime.Now;
            this.IsBusy = true;
            DateTime processDate = DateTime.Now.Date;
            try
            {
                if (this.AttendanceConfig.is_enable_sms_service)
                {

                    using (AttendanceDbContext dbContext = new AttendanceDbContext())
                    {

                        var studentAttendences = dbContext.StudentAttendences.Include(x => x.StudentSession)
                            .Include("StudentSession.Student")
                            .Where(x => x.date == processDate && x.attendence_type_id == StudentAbsentTypeId)
                            .ToList();

                        foreach (var attendance in studentAttendences)
                        {
                            if (attendance.remark != "SMS sent.")
                            {
                                if (this.SendSms(SmsType.Absent, attendance.StudentSession.Student, processDate))
                                {
                                    attendance.remark = "SMS sent.";
                                }
                                else
                                {
                                    attendance.remark = "SMS fail to send!";
                                }
                            }
                        }

                        await dbContext.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }
            finally
            {
                this.IsBusy = false;
                logger.Info($"SendAbsentSms - End Time: {DateTime.Now.ToString("MM/dd/yyyy h:mm:ss tt")}");
            }
        }

        private void MakeStaffAbsent(AttendanceDbContext dbContext)
        {
            DateTime processDate = DateTime.Now.Date;
            if (!dbContext.StaffAttendences.Any(x => x.date == processDate))
            {
                var staffs = dbContext.Staffs.Where(x => x.is_active > 0).ToList();
                int attendance_type_id = GetAttendanceType(false, dbContext);

                foreach (var staff in staffs)
                {
                    dbContext.StaffAttendences.Add(new Data.Models.StaffAttendence
                    {
                        staff_id = staff.id,
                        staff_attendance_type_id = attendance_type_id,
                        date = processDate,
                        is_active = 0,
                        remark = "",
                        created_at = DateTime.Now,
                        updated_at = DateTime.Now
                    });
                }

                dbContext.SaveChanges();
            }
        }

        private void MakeStudentAbsent(AttendanceDbContext dbContext)
        {
            DateTime processDate = DateTime.Now.Date;
            if (!dbContext.StudentAttendences.Any(x => x.date == processDate))
            {
                var students = dbContext.Students.Where(x => x.is_active == "yes")
                                   .Include(x => x.StudentSessions)
                                   .ToList();

                int attendance_type_id = GetAttendanceType(true, dbContext);

                foreach (var student in students)
                {
                    int sessionId = student.StudentSessions.Last().id;

                    dbContext.StudentAttendences.Add(new Data.Models.StudentAttendence
                    {
                        student_session_id = sessionId,
                        attendence_type_id = attendance_type_id,
                        date = processDate,
                        is_active = "no",
                        remark = "",
                        created_at = DateTime.Now,
                        updated_at = DateTime.Now
                    });
                }

                dbContext.SaveChanges();
            }

        }

        private int GetAttendanceType(bool isStudent, AttendanceDbContext dbContext)
        {
            DateTime processDate = DateTime.Now.Date;
            int attendance_type_id = isStudent ? StudentAbsentTypeId : StaffAbsentTypeId;

            if (processDate.DayOfWeek == DayOfWeek.Friday || processDate.DayOfWeek == DayOfWeek.Saturday)
            {
                attendance_type_id = isStudent ? StudentHolidayTypeId : StaffHolidayTypeId;
            }
            else
            {
                var calendarDay = dbContext.CalendarHolidays.Where(x => x.date == processDate).FirstOrDefault();

                if (calendarDay != null)
                {
                    if (calendarDay.is_holiday)
                        attendance_type_id = isStudent ? StudentHolidayTypeId : StaffHolidayTypeId;
                }
            }

            return attendance_type_id;
        }

        private void LoadAttendanceConfig()
        {
            using (AttendanceDbContext dbContext = new AttendanceDbContext())
            {
                this.AttendanceConfig = dbContext.AttendanceJobConfigs.FirstOrDefault();
            }
        }

        private bool UpdateStudentAttendanceStatus(ZKTeco.BioMatrix.Models.BioMatrixLog log, AttendanceDbContext dbContext)
        {
            DateTime processDate = DateTime.Now.Date;
            Student student = dbContext.Students.Where(x => x.admission_no == log.IndRegID)
                           .Include(x => x.StudentSessions)
                           .FirstOrDefault();

            if (student == null)
            {
                return false;
            }

            int sessionId = student.StudentSessions.Last().id;

            ///Find latest log
            var signInData = dbContext.BioMatrixLogs.Where(x => x.ind_reg_iD == log.IndRegID
                            && x.datetime_record > processDate)
                .GroupBy(x => x.ind_reg_iD)
                .Select(x => new
                {
                    RegId = x.Key,
                    SignIn = x.Min(y => y.datetime_record),
                    SignOut = x.Count() > 1 ? x.Max(y => y.datetime_record) : DateTime.MinValue,
                    IsSignInLog = x.Count() == 1,
                    PunchCount = x.Count()
                }).FirstOrDefault();

            if (signInData != null)
            {
                var attendance = dbContext.StudentAttendences.Where(x => x.student_session_id == sessionId
                                && x.date == processDate).FirstOrDefault();

                int attTypeId = DateTime.Now.TimeOfDay <= this.AttendanceConfig.shift1_late_attendance_cutoff_time ? StudentPresentTypeId : StudentLatePresentTypeId;

                if (attendance != null)
                {
                    if (signInData.IsSignInLog)
                    {
                        attendance.attendence_type_id = attTypeId; //Present=1, Late=3
                        attendance.is_active = "yes";
                        attendance.created_at = log.DateTimeRecord;
                        attendance.updated_at = log.DateTimeRecord;

                    }
                    else
                    {
                        attendance.is_active = signInData.PunchCount % 2 == 0 ? "no" : "yes";
                        attendance.updated_at = log.DateTimeRecord;
                    }
                }
                else
                {
                    //Add new record 
                    attendance = dbContext.StudentAttendences.Add(new Data.Models.StudentAttendence
                    {
                        student_session_id = sessionId,
                        attendence_type_id = StudentPresentTypeId,
                        date = processDate,
                        is_active = "yes",
                        remark = "",
                        created_at = log.DateTimeRecord,
                        updated_at = log.DateTimeRecord
                    });

                }

                if (signInData.IsSignInLog)
                {
                    if (this.SendSms(attTypeId == StudentPresentTypeId ? SmsType.Present : SmsType.Late, student, log.DateTimeRecord))
                    {
                        attendance.remark = "SMS sent.";
                    }
                    else
                    {
                        attendance.remark = "SMS fail to send!";
                    }
                }

                dbContext.SaveChanges();

            }


            return true;
        }

        private void UpdateStaffAttendanceStatus(ZKTeco.BioMatrix.Models.BioMatrixLog log, AttendanceDbContext dbContext)
        {
            DateTime processDate = DateTime.Now.Date;
            var staff = dbContext.Staffs.Where(x => x.employee_id == log.IndRegID.ToString())
                           .FirstOrDefault();

            if (staff == null)
            {
                return;
            }

            ///get BioMatrix Logs according to process date and employee id
            var signInData = dbContext.BioMatrixLogs.Where(x => x.ind_reg_iD == log.IndRegID
                            && x.datetime_record > processDate)
                .GroupBy(x => x.ind_reg_iD)
                .Select(x => new
                {
                    RegId = x.Key,
                    SignIn = x.Min(y => y.datetime_record),
                    SignOut = x.Count() > 1 ? x.Max(y => y.datetime_record) : DateTime.MinValue,
                    IsSignInLog = x.Count() == 1,
                    PunchCount = x.Count()
                }).FirstOrDefault();

            if (signInData != null)
            {
                var attendance = dbContext.StaffAttendences.Where(x => x.staff_id == staff.id
                                && x.date == processDate).FirstOrDefault();

                if (attendance != null)
                {
                    if (signInData.IsSignInLog)
                    {
                        int attTypeId = DateTime.Now.TimeOfDay <= this.AttendanceConfig.staff_late_attendance_cutoff_time ? StaffPresentTypeId : StaffLatePresentTypeId;
                        attendance.staff_attendance_type_id = attTypeId;
                        attendance.is_active = 1;
                        attendance.created_at = DateTime.Now;
                        attendance.updated_at = DateTime.Now;
                    }
                    else
                    {
                        attendance.is_active = signInData.PunchCount % 2 == 0 ? 0 : 1;
                        attendance.updated_at = DateTime.Now;
                    }
                }
                else
                {
                    dbContext.StaffAttendences.Add(new Data.Models.StaffAttendence
                    {
                        staff_id = staff.id,
                        staff_attendance_type_id = StaffPresentTypeId,
                        date = processDate,
                        is_active = 1,
                        remark = "",
                        created_at = DateTime.Now,
                        updated_at = DateTime.Now
                    });

                }
            }

            dbContext.SaveChanges();
        }


     
        private bool SendSms(SmsType smsType, Student student, DateTime dateTimeRecord)
        {
            if (this.AttendanceConfig.is_enable_sms_service)
            {
                string smsContent = this.PrepareSmsContent(smsType, student, dateTimeRecord);
                BasecampSMSSender smssender = new BasecampSMSSender("sazzadul.islam@asdbd.com", "abc987");

                string res = smssender.SendSms(student.guardian_phone, smsContent);
                return res.Contains("200");
            }

            return false;
        }

        private string PrepareSmsContent(SmsType smsType, Student student, DateTime dateTimeRecord)
        {
            string smsContent = string.Empty;
            if (smsType == SmsType.Present)
            {
                smsContent = this.AttendanceConfig.present_sms;
            }
            else if (smsType == SmsType.Late)
            {
                smsContent = this.AttendanceConfig.late_sms;
            }
            else if (smsType == SmsType.Absent)
            {
                smsContent = this.AttendanceConfig.absent_sms;

            }

            return smsContent.Replace("@[datetime]#", dateTimeRecord.ToString("dd/MM/yyyy hh:mm tt"))
                .Replace("@[date]#", dateTimeRecord.ToString("dd/MM/yyyy"))
                .Replace("@[studentname]#", student.firstname);
        }
    }
}
