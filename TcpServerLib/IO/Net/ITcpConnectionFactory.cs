#region Copyright

// Copyright © 2018 Rice Lake Weighing Systems

#endregion

namespace TcpServerLib.IO.Net
{
    public interface ITcpConnectionFactory
    {
        ITcpConnection Create(ICustomTcpClient client);
    }
}