/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 *
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
 * User: JustinFyfe
 * Date: 2019-1-22
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

namespace SanteDB.Core.Diagnostics
{
    /// <summary>
    /// Timed Trace listener
    /// </summary>
    public class RollOverTextWriterTraceListener : TraceListener
    {
        private Object s_lockObject = new object();

        string _fileName;
        System.DateTime _currentDate;
        //System.IO.StreamWriter _traceWriter;
        //FileStream _stream;

        /// <summary>
        /// Filename
        /// </summary>
        public String FileName { get { return _fileName; } }

        public RollOverTextWriterTraceListener(string fileName)
        {
            // Pass in the path of the logfile (ie. C:\Logs\MyAppLog.log)
            // The logfile will actually be created with a yyyymmdd format appended to the filename
            _fileName = fileName;
            if (!Path.IsPathRooted(fileName))
                _fileName = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
               Path.GetFileName(_fileName));
            //_stream = File.Open(generateFilename(), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
            //_stream.Seek(0, SeekOrigin.End);
            //_traceWriter = new StreamWriter(_stream);
            //_traceWriter.AutoFlush = true;
        }

        public override void Write(string value)
        {
            //checkRollover();
            lock (s_lockObject)
                using (FileStream fs = File.Open(generateFilename(), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                {
                    fs.Seek(0, SeekOrigin.End);

                    using (StreamWriter sw = new StreamWriter(fs))
                        sw.Write("{0} [@{1}] : {2}", DateTime.Now, Thread.CurrentThread.ManagedThreadId, value);
                }
            //            _traceWriter.Flush();
            //            _stream.Flush();
        }

        public override void WriteLine(string value)
        {

            lock (s_lockObject)
                using (FileStream fs = File.Open(generateFilename(), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read))
                {
                    fs.Seek(0, SeekOrigin.End);
                    using (StreamWriter sw = new StreamWriter(fs))
                        sw.WriteLine(value);
                }

            //checkRollover();
            //_traceWriter.WriteLine("{0}:{1}", DateTime.Now, value);
            //_traceWriter.Flush();
            //_stream.Flush();
        }

        private string generateFilename()
        {
            _currentDate = System.DateTime.Today;
            return Path.Combine(Path.GetDirectoryName(_fileName), Path.GetFileNameWithoutExtension(_fileName) + "_" +
               _currentDate.ToString("yyyyMMdd") + Path.GetExtension(_fileName));
        }

        private void checkRollover()
        {
            //// If the date has changed, close the current stream and create a new file for today's date
            //if (_currentDate.CompareTo(System.DateTime.Today) != 0)
            //{
            //    _traceWriter.Close();
            //    _stream.Close();
            //    _stream = File.Open(generateFilename(), FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
            //    _traceWriter = new StreamWriter(_stream);
            //    _traceWriter.AutoFlush = true;
            //}
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                //_traceWriter.Close();
            }
        }

    }
}

