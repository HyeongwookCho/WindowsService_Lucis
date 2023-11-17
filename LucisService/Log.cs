using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Diagnostics;
using LucisServiceTest;

namespace LucisService
{
    class Log
    {
        #region [전역 변수]
        // 설정 파일 경로
        private static readonly string configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logconfig.json");        
        private static string fileNameFormat = "yyyy-MM-dd_HH";
        private static object lockObj = new object(); // 잠금 객체
        #endregion

        #region [Log]
        public static void WriteLog(string str)
        {
            try
            {
                // 파일명 정하기
                DateTime logFileNameTime = DateTime.Now;
                string logFileName = logFileNameTime.ToString(fileNameFormat);
                string filepath = logFileName + ".dat";
                string directoryPath = Path.Combine(GetDirectoryPathFromConfig(), "LucisService_Log");
                string fullFilePath = Path.Combine(directoryPath, filepath);

                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                lock (lockObj)
                {
                    using (StreamWriter sw = new StreamWriter(fullFilePath, true))
                    {
                        sw.WriteLine(str);
                    }
                }
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("LucisService", ex.Message, EventLogEntryType.Error);
            }

        }
        #endregion

        #region [create / read config & directory path]
        private static string GetDirectoryPathFromConfig()
        {
            try
            {
                if (!File.Exists(configFilePath))
                {
                    File.WriteAllText(configFilePath, JsonConvert.SerializeObject(GetDefaultDrive()));
                }
                string json = File.ReadAllText(configFilePath);
                return JsonConvert.DeserializeObject<string>(json);
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("LucisService", ex.Message, EventLogEntryType.Error);
                throw;
            }
        }

        //드라이브 찾기
        private static string GetDefaultDrive()
        {
            try
            {
                if (DriveInfo.GetDrives()[0].IsReady && DriveInfo.GetDrives()[0].DriveType == DriveType.Fixed)
                {
                    return DriveInfo.GetDrives()[0].Name;
                }
                throw new Exception("No suitable drive found for logging.");
            }
            catch (Exception ex)
            {
                EventLog.WriteEntry("LucisService", ex.Message, EventLogEntryType.Error);
                throw;
            }
        }
        #endregion        

    }
}