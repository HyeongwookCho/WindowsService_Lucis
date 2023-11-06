using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using LucisService;
using Quartz;
using Quartz.Impl;

namespace LucisService
{
    public partial class Service1 : ServiceBase
    {
        #region [전역 변수] 
        
        private string timeFormat = "yyyy-MM-dd HH.mm.ss.fff";
        StdSchedulerFactory factory;
        IScheduler scheduler;
        #endregion

        public Service1()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            DateTime onStartTime = DateTime.Now;
            try
            {
                // 시작 시 수집 시작 로그 기록
                Log.WriteLog($"[{onStartTime.ToString(timeFormat)}] Start Collect System Resource!");
                fScheduler();
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("LucisService", ex.Message, EventLogEntryType.Error);
            }
        }

        protected override void OnStop()
        {
            DateTime onStopTime = DateTime.Now;
            try
            {
                // 중단 시 수집 중단 로그 기록
                Log.WriteLog($"[{onStopTime.ToString(timeFormat)}] Stop Collect System Resource!");
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("LucisService", ex.Message, EventLogEntryType.Error);
            }
        }

        #region [scheduling method]
        public async void fScheduler()
        {
            try
            {
                factory = new StdSchedulerFactory();
                scheduler = await factory.GetScheduler();
                await scheduler.Start();

                IJobDetail job = JobBuilder.Create<Job>()
                    .WithIdentity("job1", "group1")
                    .Build();

                ITrigger trigger = TriggerBuilder.Create()
                    .WithIdentity("trigger1", "group1")
                    .StartNow()
                    .WithCronSchedule("0 0/5 * 1/1 * ? *")
                    .Build();

                await scheduler.ScheduleJob(job, trigger);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("LucisService", ex.Message, EventLogEntryType.Error);
            }
        }
        #endregion
    }
    
}
