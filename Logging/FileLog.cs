using System;
using System.IO;

namespace Logging
{
    /// <summary>
    /// Write log information to a file.
    /// The current date will be appended to the file name, resulting in a new log file per day.
    /// 
    /// Log files will be automatically deleted after the specified number of days.
    /// </summary>
    public class FileLog
    {
        /// <summary>
        /// Set to true if contents of the log file should also be written to the console.
        /// </summary>
        public bool WriteToConsole { get; set; }

        /// <summary>
        /// Base path/name for log file.
        /// </summary>
        public string LogFile { get; set; }

        /// <summary>
        /// Number of days to keep log files.
        /// </summary>
        public double LogFileDays { get; set; }

        /// <summary>
        /// Create a new file log
        /// </summary>
        /// <param name="file">Base path/name for log file. See <see cref="LogFile"/></param>
        /// <param name="days">Number of days to keep log files. See <see cref="LogFileDays"/></param>
        public FileLog(string file, double days)
        {
            this.LogFile = file;
            this.LogFileDays = days;
        }

        /// <summary>
        /// Write text to the application log.
        /// Do not send error notification.
        /// </summary>
        /// <param name="logText"></param>
        public void Log(string text)
        {
            if (LogFile != null)
                lock (LogFile)
                    writeToLogFile(LogFile, text, LogFileDays);
            if (this.WriteToConsole)
                Console.WriteLine(text);
        }

        /// <summary>
        /// Write a formatting string to the application log.
        /// Do not write send error notification.
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Log(string format, params object[] args)
        {
            this.Log(string.Format(format, args));
        }

        /// <summary>
        /// Get list of existing log file paths
        /// </summary>
        /// <returns></returns>
        public string[] GetLogFilePaths()
        {
            return getLogFiles(this.LogFile);
        }

        /// <summary>
        /// Write to the log file
        /// </summary>
        /// <param name="baseFileName">Base log file name. Will have year-month-day added. Old log files will be deleted.</param>
        /// <param name="logText"></param>
        private static void writeToLogFile(string baseFileName, string logText, double logFileDays)
        {
            string output = String.Format("{0}:{1}", DateTime.Now.ToString(), logText);
            string file = formatLogFile(baseFileName, logFileDays);
            if (file != null)
            {
                try
                {
                    using (StreamWriter w = File.AppendText(file))
                    {
                        w.WriteLine(output);
                        w.Flush();
                    }
                }
                catch { }
            }
        }

        private static string[] getLogFiles(string logFileName)
        {
            string pattern = string.Format("{0}_????-??-??{1}",
                Path.GetFileNameWithoutExtension(logFileName), Path.GetExtension(logFileName)
                );
            return Directory.GetFiles(Path.GetDirectoryName(logFileName), pattern);
        }

        private static string formatLogFile(string fileName, double keepLogDays)
        {
            string result = null;

            if (fileName != null && fileName.Trim() != string.Empty)
            {
                string fileNameBase = Path.GetDirectoryName(fileName);
                if (!Directory.Exists(fileNameBase))
                    Directory.CreateDirectory(fileNameBase);
                else if (keepLogDays > 0)
                    deleteOldLogFiles(fileName, keepLogDays);
                if (!fileNameBase.EndsWith("\\"))
                    fileNameBase += "\\";
                fileNameBase += Path.GetFileNameWithoutExtension(fileName);
                string extension = Path.GetExtension(fileName);
                result = string.Format(
                    "{0}_{2}-{3:00}-{4:00}{1}",
                    fileNameBase, extension,
                    DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day
                    );
            }

            return result;
        }

        private static void deleteOldLogFiles(string logFileName, double keepLogDays)
        {
            string path = Path.GetDirectoryName(logFileName);
            string pattern = string.Format("{0}_????-??-??{1}",
                Path.GetFileNameWithoutExtension(logFileName), Path.GetExtension(logFileName)
                );
            foreach (string s in Directory.GetFiles(path, pattern))
            {
                if (DateTime.Now.Subtract(Directory.GetLastWriteTime(s)).TotalDays > keepLogDays)
                    File.Delete(s);
            }
        }
    }
}
