#region Copyright

// Copyright © 2018 Rice Lake Weighing Systems

#endregion

using System;
using System.Net.Sockets;

namespace TcpServerLib.IO.Net
{
    public class CustomTcpClientFactory : ICustomTcpClientFactory
    {
        public ICustomTcpClient Create(Socket socket)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            return new CustomTcpClient(socket);
        }
    }
}