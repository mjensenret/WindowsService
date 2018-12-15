#region Copyright

// Copyright © 2018 Rice Lake Weighing Systems

#endregion

using System.Runtime.InteropServices;
using System.Threading;

namespace TcpServerLib.Threading
{
    public sealed class WaitDelay
    {
        private readonly ManualResetEvent m_waitEvent = new ManualResetEvent(false);

        private Timer m_waitTimer;

        public static WaitDelay GetInstance()
        {
            return new WaitDelay();
        }

        public void Wait(int milliseconds)
        {
            m_waitEvent.Reset();
            m_waitTimer = new Timer(WaitTimerCallback, null, milliseconds, milliseconds);
            m_waitEvent.WaitOne();

            m_waitTimer.Dispose();
            m_waitTimer = null;
        }

        [DllImport("winmm.dll")]
        private static extern int timeGetTime();

        private void WaitTimerCallback(object state)
        {
            m_waitEvent.Set();
        }
    }
}