#region Copyright

// Copyright © 2018 Rice Lake Weighing Systems

#endregion

using System;

namespace TcpServerLib.IO.Net
{
    public class TcpConnectionFactory : ITcpConnectionFactory
    {
        private readonly IProtocol m_protocol;
        private readonly IProtocolHandler m_protocolHandler;
        private readonly int m_chunkSize;
        private readonly int m_chunkDelay;
        private readonly bool m_closeConnectionAfterResponse;

        public TcpConnectionFactory(IProtocol protocol, IProtocolHandler protocolHandler,
            int chunkSize = 25, int chunkDelay = 250, bool closeConnectionAfterResponse = false)
        {
            if (protocol == null)
            {
                throw new ArgumentNullException(nameof(protocol));
            }

            if (protocolHandler == null)
            {
                throw new ArgumentNullException(nameof(protocolHandler));
            }

            if (chunkSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(chunkSize));
            }

            if (chunkDelay <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(chunkDelay));
            }

            m_protocol = protocol;
            m_protocolHandler = protocolHandler;
            m_chunkSize = chunkSize;
            m_chunkDelay = chunkDelay;
            m_closeConnectionAfterResponse = closeConnectionAfterResponse;
        }

        public ITcpConnection Create(ICustomTcpClient client)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            return new TcpConnection(client, m_protocol, m_protocolHandler, m_chunkSize, m_chunkDelay, m_closeConnectionAfterResponse);
        }
    }
}