#region Copyright

// Copyright © 2018 Rice Lake Weighing Systems

#endregion

using System;
using System.Runtime.Serialization;

namespace TcpServerLib.IO
{
    [Serializable]
    public class ProtocolException : Exception, ISerializable
    {
        public ProtocolException()
        {
        }

        public ProtocolException(string message)
            : base(message)
        {
        }

        public ProtocolException(string message, Exception e)
            : base(message, e)
        {
        }
    }
}