#region Copyright

// Copyright © 2018 Rice Lake Weighing Systems

#endregion

namespace TcpServerLib.IO
{
    public interface IConnection
    {
        void Close();

        bool Open();

        void SendData(string data);
    }
}