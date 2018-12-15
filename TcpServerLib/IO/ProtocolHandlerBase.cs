#region Copyright

// Copyright © 2018 Rice Lake Weighing Systems

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace TcpServerLib.IO
{
    public abstract class ProtocolHandlerBase : IProtocolHandler
    {
        public const string ACK = "Y";
        public const string NAK = "N";

        private const string TEST_PATTERN = "^TEST.*$";
        private const string GET_TIME_DATE_PATTERN = @"^GET_TD\|.*";
        private const string DEVICE_ERROR_PATTERN = @"^\?\?.*$";
        protected const string PROTOCOL_ERROR_REPLY = "PROTOCOLERROR";

        private readonly Dictionary<string, Func<string, string>> m_messageActionMap =
            new Dictionary<string, Func<string, string>>();

        protected ProtocolHandlerBase()
        {
            m_messageActionMap.Add(TEST_PATTERN, ProcessTestMessage);
            m_messageActionMap.Add(GET_TIME_DATE_PATTERN, ProcessGetTimeDateMessage);
            m_messageActionMap.Add(DEVICE_ERROR_PATTERN, ProcessDeviceErrorMessage);
        }

        public virtual string ProcessMessage(string connectionInfo, string request)
        {
            Debug.WriteLine($"Received: {request} from {connectionInfo}.");

            return ProcessMessage(request);
        }

        protected void AddCommandHandler(string commandPattern, Func<string, string> handler)
        {
            m_messageActionMap.Add(commandPattern, handler);
        }

        protected static string GetDateTimePattern()
        {
            return @"[0-9 ]{13}";
        }

        protected static string GetFieldDelimiterPattern()
        {
            return @"\|";
        }

        protected static string GetRequiredStringPattern(int maxLength)
        {
            return $@"[^\|]{{1,{maxLength}}}";
        }

        protected static string GetStringPattern(int maxLength)
        {
            return $@"[^\|]{{0,{maxLength}}}";
        }

        protected static string GetWeightPattern()
        {
            return @"[0-9\,\.\- ]{1,8}";
        }

        protected virtual void PostProcessMessage(string message, string result)
        {
        }

        protected virtual void PreProcessMessage(string message)
        {
        }

        private static string ProcessDeviceErrorMessage(string message)
        {
            return string.Empty;
        }

        private static string ProcessGetTimeDateMessage(string message)
        {
            // get date and time from the PC service
            // GET_TD|<CR>
            // F#1=GET_TD|MMddyy HHmmss|<CR>

            return $"GET_TD|{DateTime.Now:MMddyy HHmmss}|";
        }

        private string ProcessMessage(string request)
        {
            PreProcessMessage(request);
            var response = PROTOCOL_ERROR_REPLY;
            foreach (var key in m_messageActionMap.Keys)
            {
                if (Regex.IsMatch(request, key, RegexOptions.Singleline))
                {
                    response = m_messageActionMap[key].Invoke(request);
                    break;
                }
            }

            response = WrapResponseInProtocolEnvelope(response);
            PostProcessMessage(request, response);

            return response;
        }

        private static string ProcessTestMessage(string message)
        {
            // test PC service connection
            // TEST<CR>
            // F#1=TEST|Y|<CR> – success

            return $"TEST|{ACK}|";
        }

        private static string WrapResponseInProtocolEnvelope(string response)
        {
            return string.IsNullOrEmpty(response)
                ? ""
                : $"F#1={response}\r";
        }

    }
}