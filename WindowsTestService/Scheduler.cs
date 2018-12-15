using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using TcpServerLib.IO;
using TcpServerLib.IO.Net;
using WindowsTestService.Properties;
using Logging;

namespace WindowsTestService
{
    public partial class Scheduler : ServiceBase
    {
        private Timer timer1 = null;

        private static readonly IServer m_tcpServer = new Server(
            new CustomTcpListenerFactory(),
            new TcpConnectionFactory(new StandardIndicatorProtocol(),
                new ScaleProtocolHandler(),
                Settings.Default.chunkSize,
                Settings.Default.chunkDelay,
                Settings.Default.closeConnectionAfterResponse),
            Settings.Default.ListeningPort,
            Settings.Default.maxConnections);
        public Scheduler()
        {
            InitializeComponent();

            eventLog = new System.Diagnostics.EventLog();
            if (!EventLog.SourceExists("ScaleSource"))
            {
                EventLog.CreateEventSource("ScaleSource", "ScaleLog");
            }
            eventLog.Source = "ScaleSource";
            eventLog.Log = "ScaleLog";
        }

        protected override void OnStart(string[] args)
        {
            //timer1 = new Timer();
            //this.timer1.Interval = 30000;
            //this.timer1.Elapsed += new System.Timers.ElapsedEventHandler(this.timer1_Tick);
            //timer1.Enabled = true;
            

            try
            {
                eventLog.WriteEntry($"Starting Scale TCP Listener on port: {Settings.Default.ListeningPort}");
                Log.WriteErrorLog($"Starting tcp server on port {Settings.Default.ListeningPort}");
                m_tcpServer.Start();
                eventLog.WriteEntry($"Scale TCP Listener started.");
                Log.WriteErrorLog("Started successfully...");
            }
            catch (Exception ex)
            {
                Log.WriteErrorLog(ex);
                Trace.WriteLine(ex);
            }
        }

        protected override void OnStop()
        {
            //timer1.Enabled = false;
            Log.WriteErrorLog("Shutting down the tcp server...");
            try
            {
                m_tcpServer.Close();
                Log.WriteErrorLog("TCP server shut down successfully");
            }
            catch (Exception ex)
            {
                Log.WriteErrorLog("There was an error shutting down the tcp server.");
                Log.WriteErrorLog(ex);
            }
            //Library.WriteErrorLog("Test windows service stopped");
        }

        private void timer1_Tick(object sender, ElapsedEventArgs e)
        {
            //some code to do every timer tick
            Log.WriteErrorLog("Timer ticked and something did something.");
        }
    }
}
