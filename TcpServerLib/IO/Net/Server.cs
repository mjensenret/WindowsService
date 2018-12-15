#region Copyright

// Copyright © 2018 Rice Lake Weighing Systems

#endregion

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;

namespace TcpServerLib.IO.Net
{
    public class Server : IServer
    {
        private const int RECLAIM_TIME = 500;

        private readonly ArrayList m_connections = ArrayList.Synchronized(new ArrayList());

        private readonly ICustomTcpListenerFactory m_listenerFactory;

        private readonly int m_maxConnections;

        private readonly int m_portNumber;

        private readonly ITcpConnectionFactory m_tcpConnectionFactory;

        private Thread m_clientReclaimThread;

        private bool m_disposed;

        private bool m_listenContinue = true;

        private ICustomTcpListener m_listener;

        private Thread m_listenerThread;

        private bool m_reclaimContinue = true;

        public Server(ICustomTcpListenerFactory listenerFactory, ITcpConnectionFactory tcpConnectionFactory,
            int portNumber, int maxConnections)
        {
            if (listenerFactory == null)
            {
                throw new ArgumentNullException(nameof(listenerFactory));
            }

            if (tcpConnectionFactory == null)
            {
                throw new ArgumentNullException(nameof(tcpConnectionFactory));
            }

            if (portNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(portNumber));
            }

            if (maxConnections <= 0 || maxConnections >= 1000)
            {
                throw new ArgumentOutOfRangeException(nameof(maxConnections));
            }

            m_listenerFactory = listenerFactory;
            m_tcpConnectionFactory = tcpConnectionFactory;
            m_portNumber = portNumber;
            m_maxConnections = maxConnections;
        }

        public void Broadcast(string message)
        {
            lock (m_connections.SyncRoot)
            {
                foreach (TcpConnection conn in m_connections)
                {
                    conn.SendData(message);
                }
            }
        }

        public void Close()
        {
            Dispose();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public void Start()
        {
            StartupServer();
        }

        protected virtual void CleanupConnection(ITcpConnection connection)
        {
            lock (m_connections.SyncRoot)
            {
                Debug.WriteLine("Cleaning up connection");
                if (connection != null)
                {
                    connection.IdleTimeout -= conn_IdleTimeout;
                    m_connections.Remove(connection);
                    connection.Close();
                }
            }
        }

        protected virtual void conn_IdleTimeout(object sender, EventArgs args)
        {
            var idleConn = sender as TcpConnection;
            if (idleConn != null)
            {
                CleanupConnection(idleConn);
            }
        }

        protected virtual void DeadListenerCheck()
        {
            if (!m_disposed && !m_listenerThread.IsAlive)
            {
                m_listener?.Stop();
                StartListener();
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (m_disposed)
            {
                return;
            }

            if (disposing)
            {
                ShutdownServer();
                GC.SuppressFinalize(this);
            }

            m_disposed = true;
        }

        protected virtual void Listen()
        {
            try
            {
                m_listener = m_listenerFactory.Create(IPAddress.Any, m_portNumber);
                m_listener.Start();

                Debug.WriteLine($"Listening on port:{m_portNumber}");

                while (m_listenContinue)
                {
                    if (m_listener.Pending())
                    {
                        lock (m_connections.SyncRoot)
                        {
                            if (m_connections.Count < m_maxConnections)
                            {
                                // blocking call...
                                ITcpConnection conn = null;
                                try
                                {
                                    conn = m_tcpConnectionFactory.Create(m_listener.Accept());
                                    conn.Begin();
                                    conn.IdleTimeout += conn_IdleTimeout;

                                    m_connections.Add(conn);

                                    Debug.WriteLine($"Client connection: {IPAddress.Parse(((IPEndPoint)conn.Client.Socket.RemoteEndPoint).Address.ToString())}");
                                }
                                catch (Exception ex)
                                {
                                    Debug.WriteLine($"Exception caught on ITcpConnection Begin(): {ex}");
                                    if (ex.InnerException != null)
                                    {
                                        Debug.WriteLine($"Inner Exception: {ex.InnerException}");
                                    }

                                    if (conn != null)
                                    {
                                        CleanupConnection(conn);
                                    }
                                }
                            }
                            else
                            {
                                Debug.WriteLine("Max Server Connections Reached.");
                                using (var client = m_listener.AcceptTcpClient())
                                {
                                    using (var writer = new StreamWriter(client.GetStream()))
                                    {
                                        writer.Write("Unable to connect. Max server connections reached.");
                                        writer.Flush();
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);

                Debug.WriteLine("Server::ShutdownServer() called");
                ShutdownServer();

                Debug.WriteLine("Server::StartupServer() called");
                StartupServer();
            }
        }

        protected virtual void Reclaim()
        {
            try
            {
                while (m_reclaimContinue)
                {
                    lock (m_connections.SyncRoot)
                    {
                        for (var i = m_connections.Count - 1; i >= 0; i--)
                        {
                            var conn = m_connections[i] as TcpConnection;
                            if (conn != null && !conn.Client.IsConnectionAlive)
                            {
                                CleanupConnection(conn);
                                Debug.WriteLine($"Reclaim Removed a Client: {m_connections.Count} remaining.");
                            }
                        }
                    }

                    Thread.Sleep(RECLAIM_TIME);
                    DeadListenerCheck();
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex);

                ShutdownServer();
                StartupServer();
            }
        }

        protected virtual void ShutdownServer()
        {
            m_reclaimContinue = false;
            if (m_clientReclaimThread != null && m_clientReclaimThread.IsAlive)
            {
                m_clientReclaimThread.Join();
            }

            m_listenContinue = false;
            m_listener.Stop();
            if (m_listenerThread != null && m_listenerThread.IsAlive)
            {
                m_listenerThread.Join();
            }

            // close all the open client connections.
            lock (m_connections.SyncRoot)
            {
                foreach (TcpConnection conn in m_connections)
                {
                    conn.Client.Close();
                }
            }
        }

        protected virtual void StartListener()
        {
            if (m_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            Debug.WriteLine("Starting Listen Socket Thread");

            m_listenerThread = new Thread(Listen)
            {
                Name = "Network Server Listen Thread",
                IsBackground = true
            };
            m_listenerThread.Start();
        }

        protected virtual void StartReclaim()
        {
            if (m_disposed)
            {
                throw new ObjectDisposedException(GetType().Name);
            }

            Debug.WriteLine("Starting Reclaim Thread");

            m_clientReclaimThread = new Thread(Reclaim)
            {
                Name = "Network Server Reclaim Thread",
                IsBackground = true
            };
            m_clientReclaimThread.Start();
        }

        protected virtual void StartupServer()
        {
            StartListener();
            StartReclaim();
        }

        ~Server()
        {
            Dispose(false);
        }
    }
}