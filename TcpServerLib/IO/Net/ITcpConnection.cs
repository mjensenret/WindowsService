#region Copyright

// Copyright © 2018 Rice Lake Weighing Systems

#endregion

using System;

namespace TcpServerLib.IO.Net
{
    public interface ITcpConnection
    {
        void Begin();

        void Close();

        event EventHandler<EventArgs> IdleTimeout;

        void SendData(string data);
        ICustomTcpClient Client { get; }
    }
}