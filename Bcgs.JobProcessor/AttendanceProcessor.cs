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
        private bool SentBiometricDeviceError = false;
        private string AdminPhoneNo = "8801711468016";
        private DateTime DailyServiceCheckSMSDate = DateTime.MinValue;

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
            this.SendDailyServiceCheckSMS();
            this.ProcessDailyAttendanceLog();
            await this.SendDailyAbsentSms();
        }

        private void SendDailyServiceCheckSMS()
        {
            try
            {
                if (DateTime.Now.Hour == 7 && DailyServiceCheckSMSDate < DateTime.Now)
                {
                    this.IsBusy = true;

                    if (this.AttendanceConfig.is_enable_sms_service)
                    {
                        string smsContent = "Attendance service running...";

                        using (ZkTecoClient bioMatrixClient = new ZkTecoClient("basecampzkteco.ddns.net"))
                        {
                            try
                            {
                                bool isConnected = bioMatrixClient.ConnectToZKTeco();
                                if (!isConnected)
                                {
                                    smsContent = "Failed to connect to the biometric device";
                                }
                            }
                            catch (Exception ex)
                            {
                                smsContent = "Failed to connect to the biometric device";
                                logger.Error(ex.Message, ex);
                            }

                        }

                        BasecampSMSSender smssender = new BasecampSMSSender("sazzadul.islam@asdbd.com", "abc987");

                        string res = smssender.SendSms("8801714042726", smsContent);
                        logger.Info($"SMS- {smsContent} {Environment.NewLine}Status: {res}");

                        DailyServiceCheckSMSDate = DateTime.Now;


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
            }
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
            logger.Info($"Initiate Daily Attendance Process - Start".ToUpper());


            this.DailyProcess_LastRunTime = DateTime.Now;
            try
            {
                using (AttendanceDbContext dbContext = new AttendanceDbContext())
                {
                    this.AttendanceConfig = dbContext.AttendanceJobConfigs.FirstOrDefault();

                    MakeStaffAbsent(dbContext, DateTime.Now.Date);
                    MakeStudentAbsent(dbContext, DateTime.Now.Date);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }
            finally
            {
                this.IsBusy = false;
                logger.Info($"Initiate Daily Attendance Process - End".ToUpper());
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

            logger.Info($"Process Attendance Log - Start".ToUpper());

            this.IsBusy = true;


            this.ProcessAttendanceLog_LastRunTime = DateTime.Now;
            DateTime lastProcessDate = DateTime.MinValue;
            ICollection<BiometricLogModel> biometricLogData = new HashSet<BiometricLogModel>();



            try
            {

                logger.Info($"Get Biometric Data - Start");


                using (ZkTecoClient biometricClient = new ZkTecoClient("basecampzkteco.ddns.net"))
                {
                    biometricLogData = biometricClient.GetBiometricData();
                    SentBiometricDeviceError = false;
                    logger.Info($"{biometricLogData.Count} records found.");
                }

                logger.Info("Get Biometric Data - End");

                using (AttendanceDbContext dbContext = new AttendanceDbContext())
                {
                    if (dbContext.BiometricLogs.Any())
                    {
                        lastProcessDate = dbContext.BiometricLogs.Max(x => x.datetime_record);
                    }

                    biometricLogData = biometricLogData.Where(x => x.DateTimeRecord > lastProcessDate)
                                        .OrderBy(x => x.DateTimeRecord).ToList();


                    try
                    {

                        foreach (var log in biometricLogData)
                        {
                            dbContext.BiometricLogs.Add(new Data.Models.BiometricLog
                            {
                                machine_id = log.MachineNumber,
                                ind_reg_iD = log.IndRegID,
                                datetime_record = log.DateTimeRecord
                            });

                        }

                        ///Save BiometricLogs
                        dbContext.SaveChanges();

                        ICollection<BiometricLog> unprocessedBiometricLogData = dbContext.BiometricLogs
                            .Where(x => !x.is_processed).OrderBy(x => x.datetime_record).ToList();

                        foreach (DateTime date in unprocessedBiometricLogData.Select(x => x.datetime_record.Date).Distinct())
                        {
                            this.MakeStudentAbsent(dbContext, date);
                            this.MakeStaffAbsent(dbContext, date);
                        }

                        foreach (var log in unprocessedBiometricLogData)
                        {
                            try
                            {
                                bool isStudentLog = this.UpdateStudentAttendanceStatus(log, dbContext);
                                if (!isStudentLog)
                                {
                                    this.UpdateStaffAttendanceStatus(log, dbContext);
                                }

                                log.is_processed = true;
                                
                                dbContext.SaveChanges();
                            }
                            catch (Exception ex)
                            {
                                logger.Info(ex.Message, ex);
                            }
                        }
                        //Update Student/Staff Attendance Status
                        dbContext.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        logger.Info(ex.Message, ex);
                    }

                }
            }
            catch (ZkTecoClientException ex)
            {
                if (!this.SentBiometricDeviceError)
                {
                    this.SentBiometricDeviceError = true;
                    this.SendServiceStetusSms(ex.Message);
                }

                logger.Error(ex.Message, ex);

            }
            catch (Exception ex)
            {
                logger.Error(ex.Message, ex);
            }
            finally
            {
                this.IsBusy = false;
                logger.Info($"Process Attendance Log - End".ToUpper());

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
            if (this.AttendanceConfig.is_enable_sms_service)
            {
                logger.Info($"Send Absent Sms - Start".ToUpper());
                this.SendAbsentSms_LastRunTime = DateTime.Now;
                this.IsBusy = true;
                DateTime processDate = DateTime.Now.Date;
                try
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
                catch (Exception ex)
                {
                    logger.Error(ex.Message, ex);
                }
                finally
                {
                    this.IsBusy = false;
                    logger.Info($"Send Absent Sms - End".ToUpper());
                }
            }
        }

        private void MakeStaffAbsent(AttendanceDbContext dbContext, DateTime processDate)
        {
            //DateTime processDate = DateTime.Now.Date;
            if (!dbContext.StaffAttendences.Any(x => x.date == processDate))
            {
                logger.Info($"Make Staff Absent (Date: {processDate}) - Start".ToUpper());
                var staffs = dbContext.Staffs.Where(x => x.is_active > 0).ToList();
                int attendance_type_id = GetAttendanceType(false, dbContext, processDate);

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
                logger.Info($"Make Staff Absent - End".ToUpper());
            }
        }

        private void MakeStudentAbsent(AttendanceDbContext dbContext, DateTime processDate)
        {
            //DateTime processDate = DateTime.Now.Date;
            if (!dbContext.StudentAttendences.Any(x => x.date == processDate))
            {
                logger.Info($"Make Student Absent (Date: {processDate}) - Start".ToUpper());

                var students = dbContext.Students.Where(x => x.is_active == "yes")
                                   .Include(x => x.StudentSessions)
                                   .ToList();

                int attendance_type_id = GetAttendanceType(true, dbContext, processDate);

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
                logger.Info($"Make Student Absent - End".ToUpper());

            }

        }

        private int GetAttendanceType(bool isStudent, AttendanceDbContext dbContext, DateTime processDate)
        {
            //DateTime processDate = DateTime.Now.Date;
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

        private bool UpdateStudentAttendanceStatus(BiometricLog log, AttendanceDbContext dbContext)
        {
            DateTime processDate = log.datetime_record.Date;
            Student student = dbContext.Students.Where(x => x.admission_no == log.ind_reg_iD)
                           .Include(x => x.StudentSessions)
                           .FirstOrDefault();

            if (student == null)
            {

                return false;
            }

            int sessionId = student.StudentSessions.Last().id;

            var signInData = dbContext.BiometricLogs.Where(x => x.ind_reg_iD == log.ind_reg_iD
                            && x.datetime_record > processDate)
                .GroupBy(x => x.ind_reg_iD)
                .Select(x => new
                {
                    RegId = x.Key,
                    SignIn = x.Min(y => y.datetime_record),
                    SignOut = x.Count() > 1 ? x.Max(y => y.datetime_record) : processDate,
                    IsSignInLog = x.Min(y => y.datetime_record) == log.datetime_record,
                    PunchCount = x.Count()
                }).FirstOrDefault();


            if (signInData != null)
            {
                logger.Info($"Record Date Time: {log.datetime_record}, Student Name: {student.firstname} {student.lastname}, ID: {log.ind_reg_iD}, Sign In: {signInData.SignIn}, Sign Out: {signInData.SignOut}, Is Sign In Log: {signInData.IsSignInLog}");

                var attendance = dbContext.StudentAttendences.Where(x => x.student_session_id == sessionId
                                && x.date == processDate).FirstOrDefault();

                int attTypeId = signInData.SignIn.TimeOfDay <= this.AttendanceConfig.shift1_late_attendance_cutoff_time ? StudentPresentTypeId : StudentLatePresentTypeId;

                if (attendance != null)
                {


                    if (signInData.IsSignInLog)
                    {
                        attendance.attendence_type_id = attTypeId; //Present=1, Late=3
                        attendance.is_active = "yes";
                        attendance.created_at = signInData.SignIn;
                        attendance.updated_at = log.datetime_record;

                    }
                    else
                    {
                        attendance.attendence_type_id = attTypeId; //Present=1, Late=3
                        attendance.is_active = signInData.PunchCount % 2 == 0 ? "no" : "yes";
                        attendance.updated_at = log.datetime_record;
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
                        created_at = log.datetime_record,
                        updated_at = log.datetime_record
                    });

                }

                if (signInData.IsSignInLog && processDate == DateTime.Now.Date)
                {
                    if (this.SendSms(attTypeId == StudentPresentTypeId ? SmsType.Present : SmsType.Late, student, log.datetime_record))
                    {
                        attendance.remark = "SMS sent.";
                    }
                    else
                    {
                        attendance.remark = "SMS fail to send!";
                    }
                }

                
            }


            return true;
        }

        private void UpdateStaffAttendanceStatus(BiometricLog log, AttendanceDbContext dbContext)
        {
            DateTime processDate = log.datetime_record.Date;
            var staff = dbContext.Staffs.Where(x => x.employee_id == log.ind_reg_iD.ToString())
                           .FirstOrDefault();

            if (staff == null)
            {
                logger.Info($"ID: {log.ind_reg_iD} not found.");
              
                return;
            }

            ///get Biometric Logs according to process date and employee id
            var signInData = dbContext.BiometricLogs.Where(x => x.ind_reg_iD == log.ind_reg_iD
                            && x.datetime_record > processDate)
                .GroupBy(x => x.ind_reg_iD)
                .Select(x => new
                {
                    RegId = x.Key,
                    SignIn = x.Min(y => y.datetime_record),
                    SignOut = x.Count() > 1 ? x.Max(y => y.datetime_record) : processDate,
                    IsSignInLog = x.Min(y => y.datetime_record) == log.datetime_record,
                    PunchCount = x.Count()
                }).FirstOrDefault();

            if (signInData != null)
            {
                logger.Info($"Record Date Time: {log.datetime_record}, Staff Name: {staff.name}, ID: {log.ind_reg_iD}, Sign In: {signInData.SignIn}, Sign Out: {signInData.SignOut}, Is Sign In Log: {signInData.IsSignInLog}");


                var attendance = dbContext.StaffAttendences.Where(x => x.staff_id == staff.id
                                && x.date == processDate).FirstOrDefault();

                if (attendance != null)
                {
                    int attTypeId = signInData.SignIn.TimeOfDay <= this.AttendanceConfig.staff_late_attendance_cutoff_time ? StaffPresentTypeId : StaffLatePresentTypeId;

                    if (signInData.IsSignInLog)
                    {
                        attendance.staff_attendance_type_id = attTypeId;
                        attendance.is_active = 1;
                        attendance.created_at = signInData.SignIn;
                        attendance.updated_at = log.datetime_record;
                    }
                    else
                    {
                        attendance.staff_attendance_type_id = attTypeId; //Present=1, Late=3
                        attendance.is_active = signInData.PunchCount % 2 == 0 ? 0 : 1;

                        attendance.updated_at = log.datetime_record;
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
                        created_at = signInData.SignIn,
                        updated_at = log.datetime_record
                    });

                }
                
            }


        }



        private bool SendSms(SmsType smsType, Student student, DateTime dateTimeRecord)
        {
            if (this.AttendanceConfig.is_enable_sms_service)
            {
                string smsContent = this.PrepareSmsContent(smsType, student, dateTimeRecord);
                BasecampSMSSender smssender = new BasecampSMSSender("sazzadul.islam@asdbd.com", "abc987");

                string res = smssender.SendSms(student.guardian_phone, smsContent);
                logger.Info($"SMS - {student.guardian_name} {student.guardian_phone} {Environment.NewLine}{smsContent} {Environment.NewLine}Status: {res}{Environment.NewLine}");

                return res.Contains("200");
            }

            return false;
        }

        private bool SendServiceStetusSms(string msg)
        {
            if (this.AttendanceConfig.is_enable_sms_service)
            {
                BasecampSMSSender smssender = new BasecampSMSSender("sazzadul.islam@asdbd.com", "abc987");

                string res = smssender.SendSms(AdminPhoneNo, msg);
                logger.Info($"SMS Notification {Environment.NewLine} {msg} {Environment.NewLine}Status: {res} {Environment.NewLine}");

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
