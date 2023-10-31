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
        protected static PerformanceCounter CPUCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        protected static PerformanceCounter MemoryCounter = new PerformanceCounter("Memory", "Available MBytes");
        private static System.Threading.Timer timer;
        static StdSchedulerFactory factory;
        static IScheduler scheduler;
        #endregion

        static void Main(string[] args)
        {
            fScheduler();
            //ReadyToStart();
            /*timer = new System.Timers.Timer();
            timer.Interval = 1000;
            timer.Elapsed += new ElapsedEventHandler(ReadyToStart);
            timer.Start();*/
            Console.ReadLine();
        }
        public static async void fScheduler()
        {
            factory = new StdSchedulerFactory();
            scheduler = await factory.GetScheduler();
            await scheduler.Start();

            // define the job and tie it to our HelloJob class
            IJobDetail job = JobBuilder.Create<CollectResource>()
                .WithIdentity("job1", "group1")
                .Build();

            // Trigger the job to run now, and then repeat every 10 seconds
            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("trigger1", "group1")
                .StartNow()
                .WithCronSchedule("0 0/2 * 1/1 * ? *")
                /*.WithSimpleSchedule(x => x
                    .WithIntervalInSeconds(10)
                    .RepeatForever())*/
                .Build();

            // Tell Quartz to schedule the job using our trigger
            await scheduler.ScheduleJob(job, trigger);
        }
        public void scStop()
        {

        }
        // 수집된 리소스 기록 메서드
        private static void PrintResource()
        {
            // =============리소스 수집 시작=============
            DateTime startTime = DateTime.Now;
            string format = "yyyy-MM-dd HH.mm.ss.fff";
            string strStartTime = startTime.ToString(format);
            SystemResource systemResource = new SystemResource();
            systemResource = GetSystemResource();

            Console.WriteLine("==============================================");
            Console.WriteLine("============System Resource===================");
            Console.WriteLine("==============================================");
            DateTime loggingTime = DateTime.Now;
            for (int i = 0; i < systemResource.driveInfo.Count; i++)
            {
                Console.WriteLine($"[{loggingTime.ToString(format)}]" // 로그 기록 시간
                    + "ServerHostName: " + systemResource.ServerName + " | "
                    + "DriveName: " + systemResource.driveInfo[i].Name + " / " // 드라이브명
                    + "TotalDiskSize: " + systemResource.driveInfo[i].TotalSize + " / " // 전체 디스크 크기
                    + "CurrentDiskUsage: " + systemResource.driveInfo[i].CurrentUsage + " / " //현재 디스크 사용량
                    + "UsageRatio: " + systemResource.driveInfo[i].UsageRatio + " | " // 사용량 비율
                    + "CPUUsageRatio: " + systemResource.CPUInfo + " | " // CPU 사용률
                    + "MemoryUsage: " + systemResource.MemoryInfo + " | " // 메모리 사용량
                    + "StartTime: " + strStartTime); // 수집을 시작한 시간
            }
            Console.WriteLine("==============================================");
            Console.WriteLine("==============================================");
            Console.WriteLine("==============================================");
        }
        //시스템 리소스 수집 메서드
        private static SystemResource GetSystemResource()
        {
            SystemResource resource = new SystemResource();

            // 1. 서버 호스트명
            resource.ServerName = Dns.GetHostName();

            // 2. 디스크 관련 정보 (드라이브명 / 전체 디스크 크기 / 현재 디스크 사용량 / 사용량 비율)                       
            foreach (DriveInfo d in DriveInfo.GetDrives())
            {
                long usage = d.TotalSize - d.AvailableFreeSpace;
                double ratio = (double)usage / d.TotalSize;

                DriveInfoDetail driveInfoDetail = new DriveInfoDetail();

                driveInfoDetail.Name = d.Name;
                driveInfoDetail.TotalSize = d.TotalSize;
                driveInfoDetail.CurrentUsage = usage;
                driveInfoDetail.UsageRatio = (ratio * 100);

                resource.driveInfo.Add(driveInfoDetail);
            }
            // 3. CPU 사용률
            CPUCounter.NextValue();
            Thread.Sleep(500); //4초간 cpu 사용률 측정 thread safe를 위해 TimeSpan의 period Time 보다 작게 설정
            resource.CPUInfo = CPUCounter.NextValue().ToString() + "%";

            // 4. Memory 사용량
            resource.MemoryInfo = (MemoryCounter.NextValue() * 1024 * 1024).ToString() + "Bytes";

            return resource;
        }

        private static bool MinuteTimeChecker()
        {
            DateTime dateTime = DateTime.Now;
            string timeCheck = dateTime.ToString("mm");
            return int.Parse(timeCheck) % 2 == 0;
        }
        /*private static bool SecondTimeChecker()
        {
            DateTime dateTime = DateTime.Now;
            string timeCheck = dateTime.ToString("ss");
            return int.Parse(timeCheck) == 0;
        }*/
        // 이 구조가 최선일까..?
        /*private static void BatchProcess()
        {
            Console.WriteLine("BatchProcess called");
            if (SecondTimeChecker())
            {
                PrintResource();
            }
            else
            {
                Console.WriteLine("SecondTimeChecker returned false");
            }
        }*/
        /*private static void ReadyToStart(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine($"5분 단위 체크 \t []Thread ID Check: " + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
            if (MinuteTimeChecker())
            {
                BatchProcess();
            }
        }*/
        private static void ReadyToStart()
        {           
            timer = new System.Threading.Timer(obj =>
            {
                Console.WriteLine($"정시 5분 인지 checking... \t []Thread ID Check: " + System.Threading.Thread.CurrentThread.ManagedThreadId.ToString());
                if (MinuteTimeChecker())
                {
                    BatchProcess();
                }
            },
            null,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(1));
        }
        //TimeSpan의 주기적 반복을 이용하는 방법은 mili 단위 초가 조금씩 밀리기 때문에 지속적으로 했을 시 부정확해질 가능성이 높다.
        private static void BatchProcess()
        {
            timer.Dispose(); // 기존 타이머 종료

            timer = new System.Threading.Timer(obj =>
            {
                PrintResource();
            },
            null,
            TimeSpan.Zero,
            TimeSpan.FromMinutes(5));
        }
    }        
}
