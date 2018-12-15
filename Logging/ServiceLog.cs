using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logging
{
    public class ServiceLog
    {
        private static ServiceLog _serviceLog;

        /// <summary>
        /// Initialize logging settings.
        /// </summary>
        /// <param name="smtpServer">SMTP server host name. Pass null to not send error emails.</param>
        /// <param name="errorEmailFrom"></param>
        /// <param name="errorEmailTo">No error emails sent if null or blank.</param>
        /// <param name="logFile">Base file name and path for logging. No logging if blank.</param>
        /// <param name="includeTrace">Include trace data in log file.</param>
        /// <param name="errorLogFile">File to store error info. Set to blank to not capture.</param>
        /// <param name="logDays">Number of days to keep log files. Set to 0 to keep indefinatly.</param>
        public static void Init(
            string smtpServer, string errorEmailFrom, string errorEmailTo,
            string logFile, string errorLogFile, bool includeTrace, int logDays
            )
        {
            Shutdown();
            _serviceLog = new ServiceLog(
                smtpServer, errorEmailFrom, errorEmailTo,
                logFile, errorLogFile, includeTrace, logDays
                );
        }

        /// <summary>
        /// Force pending notification to be sent immediatly. This method should be called
        /// before the process terminates.
        /// </summary>
        public static void Shutdown()
        {
            if (_serviceLog != null && _serviceLog.ErrorHandler != null)
                _serviceLog.ErrorHandler.Dispose();
        }

        /// <summary>
        /// Returns singleton instance of the ServiceLog object
        /// </summary>
        public static ServiceLog Default
        {
            get
            {
                if (_serviceLog == null)
                    _serviceLog = new ServiceLog();
                return _serviceLog;
            }
        }

        private FileLog _fileLog;

        private ServiceLog(
            string smtpServer, string errorEmailFrom, string errorEmailTo,
            string logFile, string errorLogFile, bool includeTrace, int logDays
            )
        {
            this._fileLog = new FileLog(logFile, logDays);
            this.IncludeTrace = includeTrace;
            this.ErrorHandler = new ErrorHandler(smtpServer, errorEmailFrom, errorEmailTo, errorLogFile);
            this.ErrorHandler.LogDays = logDays;
        }

        private ServiceLog()
        {
            this._fileLog = new FileLog(null, 0);
            this.IncludeTrace = false;
            this.ErrorHandler = null;
        }

        public double LogDays
        {
            get { return _fileLog.LogFileDays; }
            set { _fileLog.LogFileDays = value; }
        }

        public ErrorHandler ErrorHandler { get; set; }

        /// <summary>
        /// Include trace data in log file
        /// </summary>
        public bool IncludeTrace { get; set; }

        /// <summary>
        /// Set to true if the application log should be written to the console.
        /// </summary>
        public bool WriteToConsole
        {
            get { return _fileLog.WriteToConsole; }
            set { _fileLog.WriteToConsole = value; }
        }

        public string LogFile { get { return _fileLog.LogFile; } set { _fileLog.LogFile = value; } }

        /// <summary>
        /// Write text to the application log.
        /// </summary>
        /// <param name="logText"></param>
        public void Log(string text)
        {
            _fileLog.Log(text);
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
        /// Write trace data to trace log.
        /// This is for verbose logging that can be turned on/off by changing Trace option.
        /// </summary>
        /// <param name="text"></param>
        public void Trace(string text)
        {
            if (this.IncludeTrace)
                this.Log("Trace: " + text);
            else if (this.WriteToConsole)
                Console.WriteLine(text);
        }

        /// <summary>
        /// Write formatted trace data to trace log
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void Trace(string format, params object[] args)
        {
            this.Trace(string.Format(format, args));
        }

        /// <summary>
        /// Return true logger has been initialized.
        /// </summary>
        public bool HasErrorHandler
        {
            get { return this.ErrorHandler != null; }
        }

        /// <summary>
        /// Log an error.
        /// </summary>
        /// <param name="e"></param>
        public void LogError(Exception e)
        {
            if (this.ErrorHandler == null)
                throw e;

            this.ErrorHandler.LogError(e);
            try { this.Log("Error: {0}", e.Message); }
            catch { }
        }

        /// <summary>
        /// Get list of existing log file paths
        /// </summary>
        /// <returns></returns>
        public string[] GetLogFilePaths()
        {
            return _fileLog.GetLogFilePaths();
        }

        /// <summary>
        /// Get list of existing error log file paths
        /// </summary>
        /// <returns></returns>
        public string[] GetErrorLogFilePaths()
        {
            return this.ErrorHandler.ErrorLog.GetLogFilePaths();
        }

    }
}
