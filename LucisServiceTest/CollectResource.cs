using ConsoleApp1;
using Quartz;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace LucisServiceTest
{
    class CollectResource : IJob
    {
        protected static PerformanceCounter CPUCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        protected static PerformanceCounter MemoryCounter = new PerformanceCounter("Memory", "Committed Bytes");
        protected static string observingListFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ObservingList.ini");
        public async Task Execute(IJobExecutionContext context)
        {
            await PrintResource();
        }
        // 수집된 리소스 기록 메서드
        private static Task PrintResource()
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
                    + "ServerHostName: " + systemResource.ServerName + " | " // 서버 호스트명
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

            

            IniFile IniTest = new IniFile();

            IniTest.Load(observingListFilePath);

            foreach (var key in IniTest["Program_Obeserving_List"].Keys)
            {
                string processName = key.ToString();
                string processPath = IniTest["Program_Obeserving_List"][key].ToString();

                Process[] processes = Process.GetProcessesByName(IniTest["Program_Obeserving_List"][key].ToString());
                Console.WriteLine("============================");
                Console.WriteLine("processName : {0}",processName);
                Console.WriteLine("processPath : {0}", processPath);
                if (processes.Length == 0)
                {
                    Console.WriteLine($"{IniTest["Program_Obeserving_List"][key]} is Not running");
                    Process.Start(IniTest["Program_Obeserving_List"][key].ToString());
                }
                else
                {
                    Console.WriteLine($"{IniTest["Program_Obeserving_List"][key]} is Running");
                }
            }
            Console.WriteLine();
            foreach (var key in IniTest["Service_Obeserving_List"].Keys)
            {
                ServiceController service = new ServiceController(IniTest["Service_Obeserving_List"][key].ToString());
                Console.WriteLine("=============SERVICE STATUS TEST===============");

                if (service.Status.ToString().Equals("Stopped") || service.Status.ToString().Equals("Paused"))
                {
                    Console.WriteLine($"{service.ServiceName} is {service.Status}");
                    service.Start();
                    Console.WriteLine($"restart {service.ServiceName}");
                }
                else
                {
                    Console.WriteLine($"{service.ServiceName} is {service.Status}");
                }
            }

            /*Console.WriteLine("=====================================================");
            Console.WriteLine("======================Process========================");
            Console.WriteLine("=====================================================");

            Process[] processlist = Process.GetProcesses();

            foreach (Process theprocess in processlist)
            {
                Console.WriteLine("Process: {0} ID: {1}", theprocess.ProcessName, theprocess.Id);
            }

            Console.WriteLine("=====================================================");
            Console.WriteLine("======================Service========================");
            Console.WriteLine("=====================================================");
            ServiceController[] services = ServiceController.GetServices();

            foreach (ServiceController theservice in services)
            {
                Console.WriteLine("Service: {0} Status: {1}", theservice.ServiceName, theservice.Status);
            }*/
            return Task.CompletedTask;
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
            Thread.Sleep(1000); //1초간 cpu 사용률 측정
            resource.CPUInfo = CPUCounter.NextValue().ToString() + "%";

            // 4. Memory 사용량
            resource.MemoryInfo = MemoryCounter.NextValue().ToString() + " Bytes";

            return resource;
        }
    }
}
