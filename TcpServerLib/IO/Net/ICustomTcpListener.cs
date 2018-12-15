#region Copyright

// Copyright © 2018 Rice Lake Weighing Systems

#endregion

using System.Net.Sockets;

namespace TcpServerLib.IO.Net
{
    public interface ICustomTcpListener
    {
        ICustomTcpClient Accept();

        TcpClient AcceptTcpClient();

        bool Pending();

        void Start();

        void Stop();
    }
}