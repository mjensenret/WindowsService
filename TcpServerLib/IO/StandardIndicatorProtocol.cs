#region Copyright

// Copyright © 2018 Rice Lake Weighing Systems

#endregion

using System;
using System.Diagnostics;
using System.Text;

namespace TcpServerLib.IO
{
    public class StandardIndicatorProtocol : IProtocol
    {
        private const int BUFFER_MAX_SIZE = 5000;
        private const string COMMAND_TERMINATOR = "\r";

        private StringBuilder m_commandBuffer = new StringBuilder();

        public event EventHandler BufferCleared;
        public event EventHandler<CommandReceivedEventArgs> CommandReceived;

        public void Receive(ReceivedData data)
        {
            m_commandBuffer.Append(RemoveLineFeeds(data));
            ExtractCommand(data.ConnectionInfo);
        }

        private static string RemoveLineFeeds(ReceivedData data)
        {
            return data.Data.Replace("\n", "");
        }

        protected virtual void OnBufferCleared(EventArgs e)
        {
            BufferCleared?.Invoke(this, e);
        }

        protected virtual void OnCommandReceived(CommandReceivedEventArgs e)
        {
            CommandReceived?.Invoke(this, e);
        }

        private void ClearBufferIfNecessary()
        {
            if (m_commandBuffer.Length > BUFFER_MAX_SIZE)
            {
                Debug.WriteLine($"Buffer Contents on Clear: {m_commandBuffer}");

                m_commandBuffer = new StringBuilder();
                OnBufferCleared(EventArgs.Empty);
            }
        }

        private void ExtractCommand(string connectionInfo)
        {
            var contents = m_commandBuffer.ToString();
            var pos = contents.IndexOf(COMMAND_TERMINATOR, 0, StringComparison.Ordinal);

            while (pos >= 0)
            {
                var command = contents.Substring(0, pos);
                ProcessExtractedCommand(pos, connectionInfo, command);

                contents = m_commandBuffer.ToString();
                pos = contents.IndexOf(COMMAND_TERMINATOR, 0, StringComparison.Ordinal);
            }

            ClearBufferIfNecessary();
        }

        private void ProcessExtractedCommand(int position, string deviceConnection, string command)
        {
            try
            {
                TryProcessExtractedCommand(deviceConnection, command);
            }
            finally
            {
                RemoveCommandFromBuffer(position);
            }
        }

        private void RemoveCommandFromBuffer(int pos)
        {
            m_commandBuffer.Remove(0, pos + 1);
        }

        private void TryProcessExtractedCommand(string deviceConnection, string command)
        {
            OnCommandReceived(new CommandReceivedEventArgs(deviceConnection, command));
        }
    }
}