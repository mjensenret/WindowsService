#region Copyright

// Copyright © 2018 Rice Lake Weighing Systems

#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using TcpServerLib.Threading;

namespace TcpServerLib.IO.Net
{
    public class TcpConnection : ITcpConnection
    {
        private const int MAX_IDLE_TIME_MS = 1000 * 60 * 5;
        private const int READ_BUFFER_SIZE = 255;

        private readonly int m_chunkDelay;
        private readonly int m_chunkSize;
        private readonly bool m_closeAfterProtocolResponse;
        private readonly IProtocol m_protocol;
        private readonly IProtocolHandler m_protocolHandler;
        private readonly byte[] m_readBuffer = new byte[READ_BUFFER_SIZE];

        private Timer m_idleTimer;

        public TcpConnection(ICustomTcpClient client, IProtocol protocol,
            IProtocolHandler protocolHandler,
            int chunkSize = 25, int chunkDelay = 250, bool closeAfterProtocolResponse = false)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

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

            Client = client;
            m_chunkSize = chunkSize;
            m_chunkDelay = chunkDelay;
            m_closeAfterProtocolResponse = closeAfterProtocolResponse;

            m_protocol = protocol;
            m_protocol.CommandReceived += m_protocol_CommandReceived;
            m_protocolHandler = protocolHandler;
        }

        public void Begin()
        {
            m_idleTimer = new Timer(IdleTimerCb, null, MAX_IDLE_TIME_MS, MAX_IDLE_TIME_MS);

            Client.Socket.BeginReceive(
                m_readBuffer,
                0,
                READ_BUFFER_SIZE,
                SocketFlags.None,
                ReceiveCallback,
                null);
        }

        public event EventHandler<EventArgs> IdleTimeout;

        public void Close()
        {
            m_protocol.CommandReceived -= m_protocol_CommandReceived;
            Client.Close();
        }

        public void SendData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return;
            }

            Debug.WriteLine($"sending: {data}");

            var bytes = Encoding.ASCII.GetBytes(data);
            if (IsDataSmallerThanChunkSize(bytes))
            {
                DoDirectSend(bytes);
            }
            else
            {
                DoChunkedSend(bytes);
            }
        }

        public ICustomTcpClient Client { get; }

        protected virtual void DoChunkedSend(byte[] bytes)
        {
            var index = 0;
            while (index < bytes.Length)
            {
                int size;
                if (index + m_chunkSize <= bytes.Length)
                {
                    size = m_chunkSize;
                }
                else
                {
                    size = bytes.Length - index;
                }

                var chunk = new byte[size];
                Array.Copy(bytes, index, chunk, 0, size);

                lock (Client)
                {
                    Client.Client.Send(chunk, 0, chunk.Length, SocketFlags.None);
                }

                index += m_chunkSize;
                WaitDelay.GetInstance().Wait(m_chunkDelay);
            }
        }

        protected virtual void DoDirectBackgroundSend(byte[] bytes)
        {
            lock (Client)
            {
                Client.Socket.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, SendCallback, null);
            }
        }

        protected virtual void DoDirectSend(byte[] bytes)
        {
            lock (Client)
            {
                Client.Socket.Send(bytes, 0, bytes.Length, SocketFlags.None);
            }
        }

        protected virtual void m_protocol_CommandReceived(object sender, CommandReceivedEventArgs args)
        {
            try
            {
                SendData(m_protocolHandler.ProcessMessage(args.DeviceConnection, args.Command));
                if (m_closeAfterProtocolResponse)
                {
                    Close();
                }
            }
            catch (ProtocolException)
            {
                Close();
            }
        }

        protected virtual void OnIdleTimeout(EventArgs e)
        {
            IdleTimeout?.Invoke(this, e);
        }

        protected virtual void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                int bytesRead;
                lock (Client)
                {
                    bytesRead = Client.Socket.EndReceive(ar);
                }

                if (bytesRead > 0)
                {
                    // reset the idle timer
                    m_idleTimer.Change(MAX_IDLE_TIME_MS, MAX_IDLE_TIME_MS);

                    var command = Encoding.ASCII.GetString(m_readBuffer, 0, bytesRead);
                    var data = new ReceivedData(GetRemoteClientAddress(), 
                        command, DateTime.Now);

                    try
                    {
                        if (m_protocol != null)
                        {
                            m_protocol.Receive(data);
                        }
                        else
                        {
                            Console.WriteLine("NULL PROTOCOL");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Protocol Exception: {ex}");
                        SendData("INTERNAL SERVER ERROR\r\n");
                        if (m_closeAfterProtocolResponse)
                        {
                            Close();
                        }
                    }
                    finally
                    {
                        lock (Client)
                        {
                            Client.Socket?.BeginReceive(
                                m_readBuffer,
                                0,
                                READ_BUFFER_SIZE,
                                SocketFlags.None,
                                ReceiveCallback,
                                null);
                        }
                    }
                }
            }
            catch (SocketException)
            {
                //Ignored - the stream throws an exception when it's closed.
            }
            catch (Exception)
            {
                //Ignored - the stream throws an exception when it's closed.
            }
        }

        private string GetRemoteClientAddress()
        {
            return Client?.Socket?.RemoteEndPoint != null 
                ? ((IPEndPoint) Client.Socket.RemoteEndPoint).Address.ToString() 
                : "";
        }

        protected virtual void SendCallback(IAsyncResult ar)
        {
            lock (Client)
            {
                Client.Socket.EndSend(ar);
            }
        }

        private void IdleTimerCb(object state)
        {
            Close();
            OnIdleTimeout(EventArgs.Empty);
        }

        private bool IsDataSmallerThanChunkSize(IReadOnlyCollection<byte> bytes)
        {
            return bytes.Count <= m_chunkSize;
        }
    }
}