using ConsoleApp1;
using Quartz;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LucisServiceTest
{
    class CollectResource : IJob
    {
        protected static PerformanceCounter CPUCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        protected static PerformanceCounter MemoryCounter = new PerformanceCounter("Memory", "Available MBytes");

        public async Task Execute(IJobExecutionContext context)
        {
            await PrintResource();
        }
        // 수집된 리소스 기록 메서드
        private static async Task PrintResource()
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
    }
}
