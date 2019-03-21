using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SanteDB.Core.Diagnostics
{
    /// <summary>
    /// Tracer writer that writes to the console
    /// </summary>
    public class ConsoleTraceWriter : TraceWriter
    {
        // Dispatch thread
        private Thread m_dispatchThread = null;

        // True when disposing
        private bool m_disposing = false;


        // The log backlog
        private Queue<KeyValuePair<ConsoleColor, String>> m_logBacklog = new Queue<KeyValuePair<ConsoleColor, string>>(10);

        // Sync object
        private static Object s_syncObject = new object();

        /// <summary>
        /// Console trace writer
        /// </summary>
        public ConsoleTraceWriter(EventLevel filter, string initializationData) : base(filter, initializationData)
        {
            // Start log dispatch
            this.m_dispatchThread = new Thread(this.LogDispatcherLoop);
            this.m_dispatchThread.IsBackground = true;
            this.m_dispatchThread.Start();

        }

        /// <summary>
        /// Write a trace
        /// </summary>
        protected override void WriteTrace(EventLevel level, string source, string format, params object[] args)
        {
            ConsoleColor color = ConsoleColor.White;
            switch (level)
            {
                case EventLevel.Verbose:
                    if (format.Contains("PERF"))
                        color = ConsoleColor.Green;
                    else
                        color = ConsoleColor.Magenta;
                    break;
                case EventLevel.Informational:
                    color = ConsoleColor.Cyan;
                    break;
                case EventLevel.Warning:
                    color = ConsoleColor.Yellow;
                    break;
                case EventLevel.Error:
                    color = ConsoleColor.Red;
                    break;
                case EventLevel.Critical:
                    color = ConsoleColor.White;
                    Console.BackgroundColor = ConsoleColor.DarkRed;
                    break;
            }

            lock (this.m_logBacklog)
            {
                this.m_logBacklog.Enqueue(new KeyValuePair<ConsoleColor, String>(color, String.Format("[{0} {1:yyyy/MM/dd HH:mm:ss}] {2} : {3}", level, DateTime.Now, source, String.Format(format, args))));
                Monitor.Pulse(this.m_logBacklog);
            }
        }

        private void LogDispatcherLoop()
        {
            while (true)
            {
                try
                {
                    Monitor.Enter(this.m_logBacklog);
                    if (this.m_disposing) return; // shutdown dispatch
                    while (this.m_logBacklog.Count == 0)
                        Monitor.Wait(this.m_logBacklog);
                    if (this.m_disposing) return;

                    while (this.m_logBacklog.Count > 0)
                    {
                        var dq = this.m_logBacklog.Dequeue();
                        Monitor.Exit(this.m_logBacklog);
                        Console.ForegroundColor = dq.Key;
                        Console.WriteLine(dq.Value);
                        Monitor.Enter(this.m_logBacklog);
                    }
                }
                catch
                {
                    ;
                }
                finally
                {
                    Monitor.Exit(this.m_logBacklog);
                }
            }
        }

        /// <summary>
        /// Dispose of the object
        /// </summary>
        public void Dispose()
        {
            if (this.m_dispatchThread != null)
            {
                this.m_disposing = true;
                lock (this.m_logBacklog)
                    Monitor.PulseAll(this.m_logBacklog);
                this.m_dispatchThread.Join(); // Abort thread
                this.m_dispatchThread = null;
            }
        }
    }
}
