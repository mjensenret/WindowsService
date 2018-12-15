#region Copyright

// Copyright © 2018 Rice Lake Weighing Systems

#endregion

using System;

namespace TcpServerLib.IO
{
    public interface IProtocol
    {
        event EventHandler BufferCleared;
        event EventHandler<CommandReceivedEventArgs> CommandReceived;

        void Receive(ReceivedData receivedData);
    }
}