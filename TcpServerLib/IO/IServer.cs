#region Copyright

// Copyright © 2018 Rice Lake Weighing Systems

#endregion

using System;

namespace TcpServerLib.IO
{
    public interface IServer : IDisposable
    {
        void Broadcast(string message);
        void Close();

        void Start();
    }
}