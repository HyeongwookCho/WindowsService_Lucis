﻿using LucisService;
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
using static LucisService.Log;

namespace LucisService
{
    class Job : IJob
    {
        #region [전역변수]
        protected PerformanceCounter CPUCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        protected PerformanceCounter MemoryCounter = new PerformanceCounter("Memory", "Available MBytes");        
        public async Task Execute(IJobExecutionContext context)
        {
            await PrintResource();
        }
        #endregion

        #region [Collect System Resource]
        // 수집된 리소스 기록 메서드
        private Task PrintResource()
        {
            // =============리소스 수집 시작=============
            DateTime startTime = DateTime.Now;
            string timeFormat = "yyyy-MM-dd HH.mm.ss.fff";
                        
            string strStartTime = startTime.ToString(timeFormat);
            SystemResource systemResource = new SystemResource();
            systemResource = GetSystemResource();

            DateTime loggingTime = DateTime.Now;            
            string strLoggingTime = loggingTime.ToString(timeFormat);
            
            Log log = new Log();
            for (int i = 0; i < systemResource.driveInfo.Count; i++)
            {                
                string logMessage = ($"[{strLoggingTime}]" // 로그 기록 시간
                    + "ServerHostName: " + systemResource.ServerName + " | "
                    + "DriveName: " + systemResource.driveInfo[i].Name + " / " // 드라이브명
                    + "TotalDiskSize: " + systemResource.driveInfo[i].TotalSize + " / " // 전체 디스크 크기
                    + "CurrentDiskUsage: " + systemResource.driveInfo[i].CurrentUsage + " / " //현재 디스크 사용량
                    + "UsageRatio: " + systemResource.driveInfo[i].UsageRatio + " | " // 사용량 비율
                    + "CPUUsageRatio: " + systemResource.CPUInfo + " | " // CPU 사용률
                    + "MemoryUsage: " + systemResource.MemoryInfo + " | " // 메모리 사용량
                    + "StartTime: " + strStartTime); // 수집을 시작한 시간
                Log.WriteLog(logMessage);
            }
            return Task.CompletedTask;
        }

        //시스템 리소스 수집 메서드
        private SystemResource GetSystemResource()
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
            resource.MemoryInfo = (MemoryCounter.NextValue() * 1024 * 1024).ToString() + "Bytes";

            return resource;
        }
        #endregion
       
    }
}