using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Text;
using System.Threading;

namespace Logging
{
    /// <summary>
    /// Error handler logs exceptions up to 4 different ways
    /// 
    /// 1 - Email notification
    /// 2 - Write to log file
    /// 3 - Write to event log
    /// 4 - Write to database
    /// 
    /// If there is an error with any of the notification types, that error is logged
    /// via the other methods.
    /// </summary>
    public class ErrorHandler : IDisposable
    {

        #region Custom Exceptions
        protected class ErrorHandlerException : ApplicationException
        {
            public ErrorHandlerException(string message, Exception innerException)
                : base(message, innerException) { }
        }

        protected class LogFileException : ErrorHandlerException
        {
            public LogFileException(string message, Exception innerException)
                : base(message, innerException) { }
        }

        protected class EventLogException : ErrorHandlerException
        {
            public EventLogException(string message, Exception innerException)
                : base(message, innerException) { }
        }

        protected class SendEmailException : ErrorHandlerException
        {
            public SendEmailException(string message, Exception innerException)
                : base(message, innerException) { }
        }

        protected class DBException : ErrorHandlerException
        {
            public DBException(string message, Exception innerException)
                : base(message, innerException) { }
        }

        protected class AddQueueExceptoin : ErrorHandlerException
        {
            public AddQueueExceptoin(string message, Exception innerException)
                : base(message, innerException) { }
        }

        #endregion

        #region Custom Types
        private class ExceptionQueueItem
        {
            public Exception exception;
            public DateTime firstDate;
            public int count;
            public ExceptionQueueItem(Exception exception)
            {
                this.exception = exception;
                this.firstDate = DateTime.Now;
                this.count = 1;
            }
        }
        #endregion

        // Private Variables
        private string _appName = AppDomain.CurrentDomain.FriendlyName;
        private DateTime _lastSendTime = DateTime.MinValue;
        private IList<ExceptionQueueItem> _exceptionQueue = new List<ExceptionQueueItem>();
        private string _eventSource;
        private Timer _sendTimer;
        private int _timerInterval = 1000;

        /// <summary>
        /// Create error handler with default values (no logging)
        /// </summary>
        public ErrorHandler()
        {
            // Set default property values
            this.EventSource = null;
            this.SMTPHost = null;
            this.EmailFromAddress = null;
            this.EmailToAddress = null;
            this.ConnectionString = null;
            this.WriteToConsole = false;
            this.QueueMinutes = 10;
            this.ErrorLog = new FileLog(null, 30);

            _sendTimer = new Timer(sendPendingNotifications, false, _timerInterval, Timeout.Infinite);
        }

        /// <summary>
        /// Create an error handler with email notifications and write to a log file.
        /// </summary>
        /// <param name="SMTPHost"></param>
        /// <param name="EmailFromAddress"></param>
        /// <param name="EmailToAddress"></param>
        /// <param name="LogFile">If not null, log file to write errors to.</param>
        public ErrorHandler(
            string SMTPHost, string EmailFromAddress, string EmailToAddress, string LogFile
            ) : this()
        {
            this.SMTPHost = SMTPHost;
            this.EmailFromAddress = EmailFromAddress;
            this.EmailToAddress = EmailToAddress;
            this.LogFile = LogFile;
            this.LogDays = LogDays;
        }

        /// <summary>
        /// Send emails. Log errors to Application event log and default event source
        /// </summary>
        /// <param name="SMTPHost"></param>
        /// <param name="EmailFromAddress"></param>
        /// <param name="EmailToAddress"></param>
        public ErrorHandler(
            string SMTPHost, string EmailFromAddress, string EmailToAddress
            ) : this()
        {
            this.SMTPHost = SMTPHost;
            this.EmailFromAddress = EmailFromAddress;
            this.EmailToAddress = EmailToAddress;
            this.EventSource = AppDomain.CurrentDomain.FriendlyName;
        }

        /// <summary>
        /// Log to event log
        /// </summary>
        /// <param name="EventSource"></param>
        public ErrorHandler(string EventSource)
            : this()
        {
            this.EventSource = EventSource;
        }

        /// <summary>
        /// Path and file name to use for error log. Current date/time will be added to file name
        /// </summary>
        public string LogFile
        {
            get { return ErrorLog.LogFile; }
            set { ErrorLog.LogFile = value; }
        }

        /// <summary>
        /// If > 0, logs will be deleted after this many days.
        /// Default log days = 30.
        /// </summary>
        public double LogDays
        {
            get { return ErrorLog.LogFileDays; }
            set { ErrorLog.LogFileDays = value; }
        }

        /// <summary>
        /// SMTP Host for email notifications
        /// </summary>
        public string SMTPHost { get; set; }

        /// <summary>
        /// From email address for email notifications
        /// </summary>
        public string EmailFromAddress { get; set; }

        /// <summary>
        /// Email address to send email notifications to.
        /// </summary>
        public string EmailToAddress { get; set; }

        /// <summary>
        /// Connection string of database or database logging.
        /// Database must containg LogError procedure that takes
        /// the following parameters:
        /// @message varchar(1024)
        /// @stackTrace varchar(4000)
        /// @source varchar(1024)
        /// 
        /// The following parameters are also used if called within a web application
        /// @user varchar(50)
        /// @url varchar(1000)
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Event source to use when writting to Event Log
        /// </summary>
        public string EventSource
        {
            get { return _eventSource; }
            set
            {
                _eventSource = value;
                if (_eventSource != null && _eventSource != String.Empty)
                    if (!EventLog.SourceExists(_eventSource))
                        EventLog.CreateEventSource(_eventSource, null);
            }
        }

        /// <summary>
        /// If set to zero messages will be sent immediatly.
        /// 
        /// If QueueMinutes is greater than zero the first error will be sent immediatly.
        /// Additional messages will be queued until QueueMinutes has elapsed.
        /// 
        /// If QueueMinutes is less than zero messages will only be send when SendPendingEmailsNow()
        /// is called.
        /// 
        /// Default = 5 minutes.
        /// </summary>
        public double QueueMinutes { get; set; }

        /// <summary>
        /// Write error information to console.
        /// </summary>
        public bool WriteToConsole { get; set; }

        public FileLog ErrorLog { get; private set; }

        /// <summary>
        /// Log error by writing the data to a log file
        /// and sending an email notification.
        /// </summary>
        /// <param name="e"></param>
        public void LogError(Exception e)
        {
            try
            {
                if (this.WriteToConsole)
                    Console.WriteLine(String.Format("Exception: {0}", e.Message));
            }
            catch { }

            // Add exception to message queue
            if (!(e is AddQueueExceptoin))
                try { addToQueue(e); }
                catch (Exception queueException)
                {
                    if (!(e is ErrorHandlerException))
                        LogError(new AddQueueExceptoin("Error adding exception to message queue", queueException));
                }

            // Write error to log file
            if (!(e is LogFileException))
                try { writeToLogFile(e); }
                catch (Exception logException)
                {
                    if (!(e is ErrorHandlerException))
                        LogError(new LogFileException("Error writing to log file", logException));
                }

            // Write error to event log
            if (!(e is EventLogException))
                try { writeToEventLog(e); }
                catch (Exception eventException)
                {
                    if (!(e is ErrorHandlerException))
                        LogError(new EventLogException("Error writing to event log", eventException));
                }

            // Write error to database
            if (!(e is DBException))
                try { writeToDatabase(e); }
                catch (Exception dbException)
                {
                    if (!(e is ErrorHandlerException))
                        LogError(new DBException("Error writing to database", dbException));
                }

            if (!(e is ErrorHandlerException))
                sendPendingNotifications(false);
        }

        ///// <summary>
        ///// Force pending notification to be sent immediatly.
        ///// </summary>
        //public void SendPendingNotificationsNow()
        //{
        //    timerCallback(true);
        //}

        /// <summary>
        /// Return a complete error description, including nested inner exceptions
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static string GetFullErrorDescription(Exception e)
        {
            StringBuilder sb = new StringBuilder();
            if (e is ApplicationException)
                sb.Append(e.Message);
            else
                sb.AppendFormat("{0}: {1}", e.GetType().Name, e.Message);
            if (e.InnerException != null)
                RecurseError(sb, e.InnerException);
            return sb.ToString();
        }

        /// <summary>
        /// Return a complete error description, including nested inner exceptions and stack trace
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static string GetFullErrorWithStack(Exception e)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0}: {1}", e.GetType().Name, e.Message);
            if (e.InnerException != null)
                RecurseError(sb, e.InnerException);
            string stackTrace = GetNestedStackTrace(e);
            if (stackTrace != null)
            {
                sb.AppendLine();
                sb.Append(stackTrace);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Check for inner exception stack trace.
        /// Note: We don't recurse down to bottom level stack trace.
        ///       This is often less usefull.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static string GetNestedStackTrace(Exception e)
        {
            if (e is ApplicationException && e.InnerException != null && e.InnerException.StackTrace != null)
                return e.InnerException.StackTrace;
            else
                return e.StackTrace;
        }

        #region Private Methods

        private void sendPendingNotifications(object state)
        {
            bool sendNow = Convert.ToBoolean(state);

            // Disable timer to prevent overlap callback
            _sendTimer.Change(Timeout.Infinite, Timeout.Infinite);
            try
            {
                _sendPendingEmails(sendNow);
            }
            finally
            {
                // Turn timer back on
                _sendTimer.Change(_timerInterval, Timeout.Infinite);
            }
        }

        private void _sendPendingEmails(bool sendNow)
        {
            try
            {
                lock (this._exceptionQueue)
                {
                    // Determine if it's time to send an email
                    // If queueMinutes is < 0, only send if sendNow parameter is true
                    sendNow = sendNow || (
                        this.QueueMinutes >= 0 &&
                        _lastSendTime <= DateTime.Now.AddMinutes(this.QueueMinutes * -1)
                        );

                    if (_exceptionQueue.Count > 0 && sendNow)
                    {
                        // Send regular priority errors
                        string body;
                        body = getEmailBody();
                        if (body != string.Empty)
                            sendEmail(body);

                        _lastSendTime = DateTime.Now;
                        this._exceptionQueue.Clear();
                    }
                }
            }
            catch (Exception e)
            {
                LogError(new SendEmailException("Error sending email notification", e));
            }
        }

        private string getEmailBody()
        {
            bool first = true;
            StringBuilder sb = new StringBuilder();
            foreach (ExceptionQueueItem item in this._exceptionQueue)
            {
                if (first)
                    first = false;
                else
                {
                    sb.AppendLine();
                    sb.AppendLine("--------------------------------------------");
                }
                sb.AppendLine(GetFullErrorDescription(item.exception));
                if (item.count > 1)
                    sb.AppendLine(String.Format(
                        "Occurred {0} times starting {1}", item.count, item.firstDate
                        ));
                if (item.exception is ApplicationException && item.exception.InnerException != null && item.exception.InnerException.StackTrace != null)
                    sb.AppendLine(item.exception.InnerException.StackTrace);
                else if (item.exception.StackTrace != null)
                    sb.AppendLine(item.exception.StackTrace);
            }
            return sb.ToString();
        }

        private void sendEmail(string body)
        {
            SmtpClient mailClient = new SmtpClient(this.SMTPHost);
            MailMessage message = new MailMessage();
            message.From = new MailAddress(this.EmailFromAddress, _appName);
            message.Subject = this._appName + " error on " + System.Net.Dns.GetHostName();
            message.Body = body;

            // Get list of recipients
            string[] addresses = this.EmailToAddress.Replace(',', ';').Split(';');
            foreach (string addr in addresses)
                message.To.Add(addr.Trim());

            mailClient.Send(message);
        }

        private void addToQueue(Exception e)
        {
            bool found = false;
            if (this.SMTPHost != null && this.SMTPHost != string.Empty &&
                this.EmailToAddress != null && this.EmailToAddress != string.Empty)
            {

                lock (this._exceptionQueue)
                {
                    for (int i = 0; i < this._exceptionQueue.Count; i++)
                    {
                        ExceptionQueueItem item = this._exceptionQueue[i];
                        if (item.exception.Message.Equals(
                            e.Message, StringComparison.CurrentCultureIgnoreCase
                            ))
                        {
                            item.count++;
                            found = true;
                            break;
                        }
                    }
                    if (!found)
                        _exceptionQueue.Add(new ExceptionQueueItem(e));
                }
            }
        }

        private void writeToEventLog(Exception e)
        {
            if (this.EventSource != null && this.EventSource != String.Empty)
            {
                EventLog myLog = new EventLog();
                myLog.Source = this.EventSource;

                myLog.WriteEntry(GetFullErrorWithStack(e), EventLogEntryType.Error);
            }
        }

        private void writeToDatabase(Exception e)
        {
            if (this.ConnectionString != null && this.ConnectionString != string.Empty)
            {
                SqlCommand cmd = new SqlCommand();
                cmd.Connection = new SqlConnection(this.ConnectionString);
                cmd.CommandText = "LogError";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@Message", SqlDbType.VarChar, 1024)).Value =
                    e.Message.Length > 1024 ? e.Message.Substring(0, 1024) : e.Message;
                cmd.Parameters.Add(new SqlParameter("@StackTrace", SqlDbType.VarChar, 4000)).Value =
                    e.StackTrace.Length > 4000 ? e.StackTrace.Substring(0, 4000) : e.StackTrace;
                cmd.Parameters.Add(new SqlParameter("@Source", SqlDbType.VarChar, 1024)).Value =
                    e.Source.Length > 1024 ? e.Source.Substring(0, 1024) : e.Source;
                //if (System.Web.HttpContext.Current != null)
                //{
                //    string user = System.Web.HttpContext.Current.User.Identity.Name;
                //    string url = System.Web.HttpContext.Current.Request.Url.ToString();
                //    cmd.Parameters.Add(new SqlParameter("@User", SqlDbType.VarChar, 50)).Value =
                //        user.Length > 50 ? user.Substring(0, 50) : user;
                //    cmd.Parameters.Add(new SqlParameter("@Url", SqlDbType.VarChar, 1024)).Value =
                //        url.Length > 1024 ? url.Substring(0, 1024) : url;
                //}
            }
        }

        private void writeToLogFile(Exception e)
        {
            StringBuilder b = new StringBuilder();
            b.AppendLine();
            b.AppendFormat("{0} Error: {1} {2}", this._appName, DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString());
            b.AppendLine();
            b.AppendLine(GetFullErrorWithStack(e));
            b.Append("-------------------------------");
            ErrorLog.Log(b.ToString());
        }

        private static void RecurseError(StringBuilder sb, Exception e)
        {
            sb.AppendLine();
            sb.AppendFormat("{0}: {1}", e.GetType().Name, e.Message);
            if (e.InnerException != null)
                RecurseError(sb, e.InnerException);
        }
        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            _timerInterval = Timeout.Infinite;
            sendPendingNotifications(true);
        }

        #endregion
    }
}
