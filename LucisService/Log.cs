using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LucisService
{
    class Log
    {
        #region [전역 변수]
        private static string dir = @"C:\"; // 기본 파일 경로
        #endregion

        #region [Log]
        public static void WriteLog(string str)
        {
            DateTime logFileNameTime = DateTime.Now;
            string nameFormat = "yyyy-MM-dd_HH";
            string logFileName = logFileNameTime.ToString(nameFormat);

            string filepath = logFileName + ".dat";
            string fullFilePath = Path.Combine(dir, filepath);

            using (StreamWriter sw = new StreamWriter(fullFilePath, true))
            {
                sw.WriteLine(str);
            }
        }
        #endregion

        #region [create / read config]
        private void CheckConfigExist()
        {

        }
        private void ReadConfig()
        {
            /*using(StreamReader sr = new StreamReader())
            {

            }*/
        }
        #endregion
    }
}
