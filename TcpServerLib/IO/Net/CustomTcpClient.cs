#region Copyright

// Copyright © 2018 Rice Lake Weighing Systems

#endregion

using System;
using System.Net.Sockets;

namespace TcpServerLib.IO.Net
{
    public class CustomTcpClient : TcpClient, ICustomTcpClient
    {
        public CustomTcpClient(Socket socket)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            Client = socket;
            base.Active = true;
        }

        public new bool Active
        {
            get { return base.Active; }
        }

        public bool IsConnectionAlive
        {
            get
            {
                try
                {
                    return TryIsConnectionAlive();
                }
                catch
                {
                    return false;
                }
            }
        }

        public Socket Socket
        {
            get { return Client; }
        }

        private bool TryIsConnectionAlive()
        {
            return !Client.Poll(1000, SelectMode.SelectRead) || Client.Available != 0;
        }
    }
}