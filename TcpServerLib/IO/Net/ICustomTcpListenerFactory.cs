#region Copyright

// Copyright © 2018 Rice Lake Weighing Systems

#endregion

using System.Net;

namespace TcpServerLib.IO.Net
{
    public interface ICustomTcpListenerFactory
    {
        ICustomTcpListener Create(IPAddress address, int port);
    }
}