#region Copyright

// Copyright © 2018 Rice Lake Weighing Systems

#endregion

using System;

namespace TcpServerLib.IO
{
    public class CommandReceivedEventArgs : EventArgs
    {
        public CommandReceivedEventArgs(string deviceConnection, string command)
        {
            Command = command;
            DeviceConnection = deviceConnection;
        }

        public string Command { get; }

        public string DeviceConnection { get; }
    }
}