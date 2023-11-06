using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Timers;
using System.Threading;
using Quartz;
using Quartz.Impl;
using LucisServiceTest;

namespace ConsoleApp1
{
    #region [시스템 리소스 참조 클래스]
    public class DriveInfoDetail
    {
        public string Name { get; set; }
        public long TotalSize { get; set; }
        public long CurrentUsage { get; set; }
        public double UsageRatio { get; set; }
    }
    public class SystemResource
    {
        public string ServerName { get; set; }
        public List<DriveInfoDetail> driveInfo = new List<DriveInfoDetail>();
        public string CPUInfo { get; set; }
        public string MemoryInfo { get; set; }
    }
    #endregion
    class Program
    {
        #region [전역 변수]        
        static StdSchedulerFactory factory;
        static IScheduler scheduler;
        #endregion

        static void Main(string[] args)
        {
            Console.WriteLine("Start Scheduler unit test!");
            fScheduler();            
            Console.ReadLine();
        }
        public static async void fScheduler()
        {
            factory = new StdSchedulerFactory();
            scheduler = await factory.GetScheduler();
            await scheduler.Start();

            IJobDetail job = JobBuilder.Create<CollectResource>()
                .WithIdentity("job1", "group1")
                .Build();

            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("trigger1", "group1")
                .StartNow()
                .WithCronSchedule("0 0/1 * 1/1 * ? *")                
                .Build();

            await scheduler.ScheduleJob(job, trigger);
        }        
    }        
}
