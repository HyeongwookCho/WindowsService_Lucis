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
using System.ServiceProcess;

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
            // ini Test
            string observingListFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ObservingList.ini");

            IniFile ini = new IniFile();
            ini["Obeserving_List"]["Program"] = "notepad";
            ini["Obeserving_List"]["List"] = "mspaint";
            ini["Obeserving_List"]["Service"] = "lucisservice";
            ini["Obeserving_List"]["Service1"] = "wmplayer";
            ini["Obeserving_List"]["Service2"] = "powershell";
            ini.Save(observingListFilePath);
            
            ini.Load(observingListFilePath);
                        
            foreach (var key in ini["Obeserving_List"].Keys)
            {
                Process[] processes = Process.GetProcessesByName(ini["Obeserving_List"][key].ToString());
                Console.WriteLine("============================");
                if (processes.Length == 0)
                {
                    Console.WriteLine($"{ini["Obeserving_List"][key]} is Not running");
                    Process.Start(ini["Obeserving_List"][key].ToString());
                }
                else
                {
                    Console.WriteLine($"{ini["Obeserving_List"][key]} is Running");
                }                
            }


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
