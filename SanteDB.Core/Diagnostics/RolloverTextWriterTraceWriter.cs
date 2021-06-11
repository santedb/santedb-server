/*
 * Portions Copyright 2019-2020, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); you 
 * may not use this file except in compliance with the License. You may 
 * obtain a copy of the License at 
 * 
 * http://www.apache.org/licenses/LICENSE-2.0 
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
 * License for the specific language governing permissions and limitations under 
 * the License.
 * 
 * User: fyfej (Justin Fyfe)
 * Date: 2019-11-27
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Configuration;
using System.Diagnostics.Tracing;
using System.Collections.Concurrent;
using SanteDB.Core.Diagnostics;

namespace SanteDB.Server.Core.Diagnostics
{
    /// <summary>
    /// Timed Trace listener
    /// </summary>
    public class RolloverTextWriterTraceWriter : TraceWriter, IDisposable
    {

        // Dispatch thread
        private Thread m_dispatchThread = null;

        // True when disposing
        private bool m_disposing = false;

        // The text writer
        private String m_logFile;

        // The log backlog
        private ConcurrentQueue<String> m_logBacklog = new ConcurrentQueue<string>();

        // Reset event
        private ManualResetEventSlim m_resetEvent = new ManualResetEventSlim(false);

        // File name reference
        private string m_fileName;

        System.DateTime _currentDate;
        //System.IO.StreamWriter _traceWriter;
        //FileStream _stream;

        /// <summary>
        /// Filename
        /// </summary>
        public String FileName { get { return this.m_fileName; } }

        /// <summary>
        /// Rollover text writer ctor
        /// </summary>
        public RolloverTextWriterTraceWriter(EventLevel filter, string fileName) : base(filter, fileName)
        {
            // Pass in the path of the logfile (ie. C:\Logs\MyAppLog.log)
            // The logfile will actually be created with a yyyymmdd format appended to the filename
            this.m_fileName = fileName;
            if (!Path.IsPathRooted(fileName))
                this.m_fileName = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
               Path.GetFileName(this.m_fileName));
            //_stream = File.Open(generateFilename(), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
            //_stream.Seek(0, SeekOrigin.End);
            //_traceWriter = new StreamWriter(_stream);
            //_traceWriter.AutoFlush = true;

            // Start log dispatch
            this.m_dispatchThread = new Thread(this.LogDispatcherLoop);
            this.m_dispatchThread.IsBackground = true;
            this.m_dispatchThread.Start();

            this.WriteTrace(EventLevel.Informational, "Startup", "{0} Version: {1} logging at level [{2}]", Assembly.GetEntryAssembly().GetName().Name, Assembly.GetEntryAssembly().GetName().Version, filter);
        }

        /// <summary>
        /// Write a trace log
        /// </summary>
        protected override void WriteTrace(EventLevel level, string source, string format, params object[] args)
        {
            this.m_logBacklog.Enqueue(String.Format("{0}@{1} <{2}> [{3:o}]: {4}", source, Thread.CurrentThread.Name, level, DateTime.Now, String.Format(format, args)));
            //string dq = String.Format("{0}@{1} <{2}> [{3:o}]: {4}", source, Thread.CurrentThread.Name, level, DateTime.Now, String.Format(format, args));
            //using (TextWriter tw = File.AppendText(this.m_logFile))
            //    tw.WriteLine(dq); // This allows other threads to add to the write queue

            this.m_resetEvent.Set();
        }


        /// <summary>
        /// Generate the file name
        /// </summary>
        private string GenerateFilename()
        {
            _currentDate = System.DateTime.Today;
            return Path.Combine(Path.GetDirectoryName(this.m_fileName), Path.GetFileNameWithoutExtension(this.m_fileName) + "_" +
               _currentDate.ToString("yyyyMMdd") + Path.GetExtension(this.m_fileName));
        }


        /// <summary>
        /// Log dispatcher loop.
        /// </summary>
        private void LogDispatcherLoop()
        {
            while (true)
            {
                try
                {
                    if (this.m_disposing) return; // shutdown dispatch
                    while (this.m_logBacklog.IsEmpty && !this.m_disposing)
                    {
                        this.m_resetEvent.Wait();
                        this.m_resetEvent.Reset();
                    }
                    if (this.m_disposing) return;

                    // Use file stream
                    using (FileStream fs = File.Open(this.GenerateFilename(), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                    {
                        fs.Seek(0, SeekOrigin.End);
                        using (StreamWriter sw = new StreamWriter(fs))
                        {
                            while(!this.m_logBacklog.IsEmpty)
                                if (this.m_logBacklog.TryDequeue(out var dq))
                                    sw.WriteLine(dq); // This allows other threads to add to the write queue
                        }
                    }
                }
                catch
                {
                    ;
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
                this.m_resetEvent.Set();
               // this.m_dispatchThread.Join(); // Abort thread
                this.m_dispatchThread = null;
            }
        }

        /// <summary>
        /// Trace data trace
        /// </summary>
        public override void TraceEventWithData(EventLevel level, string source, string message, object[] data)
        {
            foreach(var itm in data)
            {
                // TODO: 
                this.WriteTrace(level, source, String.Format("++++ DATA ++++ \r\n{0}\r\n-----\r\n{1}\r\n----\r\n+++ END DATA +++", message, itm));
            }
        }
    }

}

