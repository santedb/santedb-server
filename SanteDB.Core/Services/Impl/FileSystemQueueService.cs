/*
 * Portions Copyright 2019-2021, Fyfe Software Inc. and the SanteSuite Contributors (See NOTICE)
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
 * Date: 2021-8-5
 */
using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Model.Serialization;
using SanteDB.Core.Services;
using SanteDB.Server.Core.Configuration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace SanteDB.Server.Core.Services.Impl
{
    /// <summary>
    /// Represents a file system queue that monitors directories
    /// </summary>
    [ServiceProvider("File System Message Queue", Configuration = typeof(FileSystemQueueConfigurationSection))]
    public class FileSystemQueueService : IPersistentQueueService, IDisposable
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "File System Message Queue";

        /// <summary>
        /// Queue entry
        /// </summary>
        [XmlType(nameof(QueueEntry), Namespace = "http://santedb.org/fsqueue")]
        [XmlRoot(nameof(QueueEntry), Namespace = "http://santedb.org/fsqueue")]
        public class QueueEntry
        {
            /// <summary>
            /// Data contained
            /// </summary>
            [XmlAttribute("type")]
            public String Type { get; set; }

            /// <summary>
            /// Creation time
            /// </summary>
            [XmlAttribute("creationTime")]
            public DateTime CreationTime { get; set; }

            /// <summary>
            /// Gets the data
            /// </summary>
            [XmlText]
            public byte[] XmlData { get; set; }

            /// <summary>
            /// Save the data to a stream
            /// </summary>
            public static QueueEntry Create(Object data)
            {
                XmlSerializer xsz = XmlModelSerializerFactory.Current.CreateSerializer(data.GetType());
                using (var ms = new MemoryStream())
                {
                    xsz.Serialize(ms, data);
                    return new QueueEntry()
                    {
                        Type = data.GetType().AssemblyQualifiedName,
                        XmlData = ms.ToArray(),
                        CreationTime = DateTime.Now
                    };
                }
            }

            /// <summary>
            /// To object data
            /// </summary>
            public object ToObject()
            {
                XmlSerializer xsz = XmlModelSerializerFactory.Current.CreateSerializer(System.Type.GetType(this.Type));
                using (var ms = new MemoryStream(this.XmlData))
                {
                    return xsz.Deserialize(ms);
                }
            }

            /// <summary>
            /// Load from stream
            /// </summary>
            public static QueueEntry Load(Stream str)
            {
                var xsz = XmlModelSerializerFactory.Current.CreateSerializer(typeof(QueueEntry));
                return xsz.Deserialize(str) as QueueEntry;
            }

            /// <summary>
            /// Save the queue entry on the stream
            /// </summary>
            public void Save(Stream str)
            {
                var xsz = XmlModelSerializerFactory.Current.CreateSerializer(typeof(QueueEntry));
                xsz.Serialize(str, this);
            }
        }

        // Queue root directory
        private FileSystemQueueConfigurationSection m_configuration;

        // Watchers
        private Dictionary<String, IDisposable> m_watchers = new Dictionary<string, IDisposable>();

        /// <summary>
        /// Queue file
        /// </summary>
        private Tracer m_tracer = new Tracer(SanteDBConstants.QueueTraceSourceName);

        /// <summary>
        /// Initializes the file system queue
        /// </summary>
        public FileSystemQueueService(IConfigurationManager configurationManager)
        {
            this.m_configuration = configurationManager.GetSection<FileSystemQueueConfigurationSection>();
            if (!Directory.Exists(this.m_configuration.QueuePath))
                Directory.CreateDirectory(this.m_configuration.QueuePath);
        }

        /// <summary>
        /// Fired when a new object is queued
        /// </summary>
        public event EventHandler<PersistentQueueEventArgs> Queued;

        /// <summary>
        /// De-queue the object
        /// </summary>
        public object Dequeue(string queueName)
        {

            if (String.IsNullOrEmpty(queueName))
                throw new ArgumentNullException(nameof(queueName));

            // Open the queue
            this.Open(queueName);

            String queueDirectory = Path.Combine(this.m_configuration.QueuePath, queueName);

            // Serialize
            var queueFile = Directory.GetFiles(queueDirectory).FirstOrDefault();
            if (queueFile == null) return null;

            this.m_tracer.TraceInfo("Will dequeue {0}", Path.GetFileNameWithoutExtension(queueFile));
            object retVal = null;
            using (var fs = File.OpenRead(queueFile))
            {
                retVal = QueueEntry.Load(fs).ToObject();
            }
            File.Delete(queueFile);
            return retVal;
        }


        /// <summary>
        /// Queue an item to the queue
        /// </summary>
        public void Enqueue(string queueName, object data)
        {
            if (String.IsNullOrEmpty(queueName))
                throw new ArgumentNullException(nameof(queueName));
            else if (data == null)
                throw new ArgumentNullException(nameof(data));

            // Open the queue
            this.Open(queueName);

            String queueDirectory = Path.Combine(this.m_configuration.QueuePath, queueName);

            // Serialize
            long tick = DateTime.Now.Ticks;
            String fname = tick.ToString("00000000000000000000"),
                filePath = Path.Combine(queueDirectory, fname);
            // Prevent dups
            while (File.Exists(filePath))
            {
                tick++;
                fname = tick.ToString("00000000000000000000");
                filePath = Path.Combine(queueDirectory, fname);
            }

            using (var fs = File.Create(filePath))
                QueueEntry.Create(data).Save(fs);
            this.m_tracer.TraceInfo("Successfulled queued {0}", fname);
        }

        /// <summary>
        /// Create a directory and subscribe to it
        /// </summary>
        public void Open(string queueName)
        {
            if (this.m_watchers.ContainsKey(queueName))
                return; // already open

            String queueDirectory = Path.Combine(this.m_configuration.QueuePath, queueName);
            if (!Directory.Exists(queueDirectory))
                Directory.CreateDirectory(queueDirectory);

            this.m_tracer.TraceInfo("Opening queue {0}... Exhausing existing items...", queueDirectory);

            // Watchers
            lock (this.m_watchers)
            {
                var fsWatch = new FileSystemWatcher(queueDirectory, "*");
                fsWatch.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.CreationTime;
                fsWatch.Created += (o, e) =>
                {
                    try
                    {
                        this.Queued?.Invoke(this, new PersistentQueueEventArgs(queueName, Path.GetFileNameWithoutExtension(e.FullPath)));
                    }
                    catch (Exception ex)
                    {
                        this.m_tracer.TraceEvent(EventLevel.Error,  "FileSystem Watcher reported error on queue (Changed) -> {0}", ex);
                    }

                };
                fsWatch.Changed += (o, e) =>
                {
                    try
                    {
                        this.Queued?.Invoke(this, new PersistentQueueEventArgs(queueName, Path.GetFileNameWithoutExtension(e.FullPath)));
                    }
                    catch (Exception ex)
                    {
                        this.m_tracer.TraceEvent(EventLevel.Error,  "FileSystem Watcher reported error on queue (Changed) -> {0}", ex);
                    }
                };
                fsWatch.EnableRaisingEvents = true;
                this.m_watchers.Add(queueName, fsWatch);
            }

            // If there's anything in the directory notify
            foreach (var itm in Directory.GetFiles(queueDirectory, "*"))
            {
                this.m_tracer.TraceInfo(">>++>> {0}", Path.GetFileNameWithoutExtension(itm));
                this.Queued?.Invoke(this, new PersistentQueueEventArgs(queueName, Path.GetFileNameWithoutExtension(itm)));
            }


        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            foreach (var itm in this.m_watchers)
            {
                this.m_tracer.TraceInfo("Disposing queue {0}", itm.Key);
                itm.Value.Dispose();
            }
        }
    }
}
