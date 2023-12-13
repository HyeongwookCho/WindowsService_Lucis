using Quartz;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LucisService
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
    public class ProgramStatus
    {
        public string Name { get; set; }
        public string Status { get; set; }
    }

    public class ServiceStatus
    {
        public string Name { get; set; }
        public string Status { get; set; }
    }


    #endregion
    class Job : IJob
    {
        #region [전역변수]
        protected PerformanceCounter CPUCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        protected PerformanceCounter MemoryCounter = new PerformanceCounter("Memory", "Committed Bytes");
        protected string observingListFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ObservingList.ini");
        private List<ProgramStatus> programStatusList;
        private List<ServiceStatus> serviceStatusList;
        #endregion

        //설정된 주기로 반복할 동작
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                await PrintResource();
                await RequestPost();
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("LucisService", ex.Message, EventLogEntryType.Error);
            }
        }

        #region [Collect System Resource]
        // 수집된 리소스 기록 메서드
        private Task PrintResource()
        {
            try
            {
                // =============리소스 수집 시작=============
                DateTime startTime = DateTime.Now;                
                string timeFormat = "yyyy-MM-dd HH.mm.ss.fff";
                string strStartTime = startTime.ToString(timeFormat);

                SystemResource systemResource = new SystemResource();
                systemResource = GetSystemResource();
                string ObservingListStatus = GetObservingListStatus();
                                                
                //Log.WriteLog("===================== SYSTEM RESOURCE ===========================");
                for (int i = 0; i < systemResource.driveInfo.Count; i++)
                {
                    // =============수집 리소스 기록=============
                    DateTime loggingTime = DateTime.Now;
                    string strLoggingTime = loggingTime.ToString(timeFormat);

                    string logMessage = (
                        $"[{strLoggingTime}]" // 로그 기록 시간
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
                Log.WriteLog(ObservingListStatus);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("LucisService", ex.Message, EventLogEntryType.Error);
                throw;
            }
        }

        //시스템 리소스 수집 메서드
        private SystemResource GetSystemResource()
        {
            try
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
            catch (Exception ex)
            {
                EventLog.WriteEntry("LucisService", ex.Message, EventLogEntryType.Error);
                throw;
            }
        }
        #endregion

        #region [Observing program&service app status]
        private string GetObservingListStatus()
        {
            IniFile Ini = new IniFile();
            StringBuilder sb = new StringBuilder();            

            Ini.Load(observingListFilePath);

            //sb.AppendLine("==============PROGRAM STATUS==================");
            foreach (var key in Ini["Program_Obeserving_List"].Keys)
            {
                string processPath = Ini["Program_Obeserving_List"][key].ToString();
                string processName = Path.GetFileNameWithoutExtension(processPath);

                Process[] processes = Process.GetProcessesByName(processName);
                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss.fff");
                if (processes.Length == 0)
                {
                    sb.Append($"[{currentTime}] {processName} : Dead");
                    Process.Start(processName);
                }
                else
                {
                    sb.AppendLine($"[{currentTime}] {processName} : Alive");
                }
            }
            //sb.AppendLine("==============SERVICE STATUS==================");
            foreach (var key in Ini["Service_Obeserving_List"].Keys)
            {
                ServiceController service = new ServiceController(Ini["Service_Obeserving_List"][key].ToString());
                string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss.fff");
                if (service.Status.ToString().Equals("Stopped") || service.Status.ToString().Equals("Paused"))
                {
                    sb.Append($"[{currentTime}] {service.ServiceName} : Dead");
                    service.Start();
                }
                else
                {
                    sb.AppendLine($"[{currentTime}] {service.ServiceName} : Alive");
                }
            }

            // 여기서부터는 데이터 전송을 위한 각각의 객체(ProgramStatus, ServiceStatus)에 담아두는 동작
            programStatusList = new List<ProgramStatus>();
            serviceStatusList = new List<ServiceStatus>(); 
            
            foreach (var key in Ini["Program_Obeserving_List"].Keys)
            {
                string processName = Path.GetFileNameWithoutExtension(Ini["Program_Obeserving_List"][key].ToString());
                string status = Process.GetProcessesByName(processName).Length > 0 ? "Alive" : "Dead";
                programStatusList.Add(new ProgramStatus { Name = processName, Status = status });
            }

            foreach (var key in Ini["Service_Obeserving_List"].Keys)
            {
                string serviceName = Ini["Service_Obeserving_List"][key].ToString();
                string status = new ServiceController(serviceName).Status.ToString();
                serviceStatusList.Add(new ServiceStatus { Name = serviceName, Status = status });
            }
            return sb.ToString();
        }
        #endregion

        #region [REST API / Send JSON to Tomcat]        

        private async Task RequestPost()
        {            
            SystemResource sr = GetSystemResource();
            var data = new
            {
                systemResource = new
                {
                    serverName = sr.ServerName,
                    driveInfo = sr.driveInfo,
                    CPUInfo = sr.CPUInfo,
                    memoryInfo = sr.MemoryInfo
                },
                programStatus = programStatusList,
                serviceStatus = serviceStatusList
            };           
            
            using (var client = new HttpClient())
            {
                var jsonInputString = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                Log.WriteLog("시스템 리소스 JSON 변환 성공");
                
                var content = new StringContent(jsonInputString, Encoding.UTF8, "application/json");
                Log.WriteLog("문자열을 HTTPContent로 변환 성공");
                
                Log.WriteLog("POST 요청");
                var response = await client.PostAsync("http://localhost:8080/demo_war_exploded/system-resource", content);
                Log.WriteLog("POST 성공");
                
                var responseString = await response.Content.ReadAsStringAsync();
                Log.WriteLog("응답을 받습니다. \n" + responseString);
            }
        }

        #endregion
    }
}
