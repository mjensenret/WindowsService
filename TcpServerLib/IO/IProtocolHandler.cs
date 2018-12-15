#region Copyright

// Copyright © 2018 Rice Lake Weighing Systems

#endregion

namespace TcpServerLib.IO
{
    public interface IProtocolHandler
    {
        string ProcessMessage(string connectionInfo, string request);
    }
}