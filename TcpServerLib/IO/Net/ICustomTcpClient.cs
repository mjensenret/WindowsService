#region Copyright

// Copyright © 2018 Rice Lake Weighing Systems

#endregion

using System.Net.Sockets;

namespace TcpServerLib.IO.Net
{
    public interface ICustomTcpClient
    {
        void Close();
        bool Active { get; }

        Socket Client { get; }

        bool IsConnectionAlive { get; }

        Socket Socket { get; }
    }
}