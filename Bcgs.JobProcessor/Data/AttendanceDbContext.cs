

using Bcgs.JobProcessor.Data.Models;
using MySql.Data.Entity;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Text;



namespace Bcgs.JobProcessor.Data
{

    [DbConfigurationType(typeof(MySqlEFConfiguration))]
    public class AttendanceDbContext : DbContext
    {
        public DbSet<Staff> Staffs { get; set; }
        public DbSet<AttendanceJobConfig> AttendanceJobConfigs { get; set; }
        public DbSet<StudentAttendence> StudentAttendences { get; set; }
        public DbSet<StaffAttendence> StaffAttendences { get; set; }
        public DbSet<BioMatrixLog> BioMatrixLogs { get; set; }
        public DbSet<StudentSession> StudentSessions { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<CalendarHoliday> CalendarHolidays { get; set; }

        public AttendanceDbContext() : base()
        {

        }

        // Constructor to use on a DbConnection that is already opened
        public AttendanceDbContext(DbConnection existingConnection, bool contextOwnsConnection)
          : base(existingConnection, contextOwnsConnection)
        {

        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CalendarHoliday>()
               .ToTable("calendar_holiday")
               .HasKey(e => e.id);


            modelBuilder.Entity<AttendanceJobConfig>()
               .ToTable("attendance_job_config")
               .HasKey(e => e.id);

            modelBuilder.Entity<StudentAttendence>()
                .ToTable("student_attendences")
                .HasKey(e => e.id)
                .HasRequired<StudentSession>(s => s.StudentSession)
                .WithMany(g => g.StudentAttendences)
                .HasForeignKey<int>(s => s.student_session_id);

            modelBuilder.Entity<StaffAttendence>()
               .ToTable("staff_attendance")
               .HasKey(e => e.id);

            modelBuilder.Entity<Student>()
                .ToTable("students")
                .HasKey(e => e.id);

            modelBuilder.Entity<StudentSession>()
                .ToTable("student_session")
                .HasKey(e => e.id)
                .HasRequired<Student>(s => s.Student)
                .WithMany(g => g.StudentSessions)
                .HasForeignKey<int>(s => s.student_id);

            modelBuilder.Entity<BioMatrixLog>()
                .ToTable("biomatrix_log")
                .HasKey(e => e.id);

            modelBuilder.Entity<Staff>()
                .ToTable("staff")
                .HasKey(e => e.id);
        }
    }

   
}