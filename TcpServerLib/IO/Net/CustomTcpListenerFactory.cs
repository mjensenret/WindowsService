#region Copyright

// Copyright © 2018 Rice Lake Weighing Systems

#endregion

using System;
using System.Net;

namespace TcpServerLib.IO.Net
{
    public class CustomTcpListenerFactory : ICustomTcpListenerFactory
    {  
        public ICustomTcpListener Create(IPAddress address, int port)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            return new CustomTcpListener(new CustomTcpClientFactory(), address, port);
        }
    }
}