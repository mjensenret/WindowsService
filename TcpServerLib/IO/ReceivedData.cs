#region Copyright

// Copyright © 2018 Rice Lake Weighing Systems

#endregion

using System;

namespace TcpServerLib.IO
{
    public class ReceivedData
    {
        public ReceivedData(string connectionInfo, string data, DateTime receiveDateTime)
        {
            ConnectionInfo = connectionInfo;
            Data = data;
            ReceiveDateTime = receiveDateTime;
        }

        public string ConnectionInfo { get; }

        public string Data { get; }

        public DateTime ReceiveDateTime { get; }
    }
}