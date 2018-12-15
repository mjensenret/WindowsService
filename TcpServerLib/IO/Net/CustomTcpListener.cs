#region Copyright

// Copyright © 2018 Rice Lake Weighing Systems

#endregion

using System;
using System.Net;
using System.Net.Sockets;

namespace TcpServerLib.IO.Net
{
    public class CustomTcpListener : TcpListener, ICustomTcpListener
    {
        private readonly ICustomTcpClientFactory m_clientFactory;

        public CustomTcpListener(ICustomTcpClientFactory clientFactory, IPAddress address, int port)
            : base(address, port)
        {
            if (clientFactory == null)
            {
                throw new ArgumentNullException(nameof(clientFactory));
            }

            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (port <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            m_clientFactory = clientFactory;
        }

        public ICustomTcpClient Accept()
        {
            return m_clientFactory.Create(AcceptSocket());
        }
    }
}