/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: fyfej
 * Date: 2022-5-30
 */
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Export;
using SanteDB.Core.Model.Interfaces;
using SanteDB.Core.Model.Serialization;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace SanteDB.Server.Core.Persistence
{
    /// <summary>
    /// Data initialization service
    /// </summary>
    /// <remarks>
    /// <para>This service reads data from the configured directory (usually the <c>data/</c> directory) with extension <c>.dataset</c>
    /// and imports the data into the SanteDB iCDR instance. Each <see cref="Dataset"/> file is then renamed to <c>.completed</c> to 
    /// indicate that the import of the provided data was successful. This service allows SanteDB instances to share information between
    /// deployments easily.</para>
    /// <para>For more information consult the <see href="https://help.santesuite.org/developers/applets/distributing-data">Distributing Data</see> files</para>
    /// </remarks>
    /// <seealso cref="Dataset"/>
    [ServiceProvider("Dataset Installation Service")]
    public class DataInitializationService : IDaemonService, IReportProgressChanged
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "DataSet Initialization Service";

        // Trace source
        private readonly Tracer m_traceSource = Tracer.GetTracer(typeof(DataInitializationService));

        /// <summary>
        /// True when the service is running
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Fired when the service has started
        /// </summary>
        public event EventHandler Started;

        /// <summary>
        /// Fired when the service is starting
        /// </summary>
        public event EventHandler Starting;

        /// <summary>
        /// Fired when the service has stopped
        /// </summary>
        public event EventHandler Stopped;

        /// <summary>
        /// Fired when the service is stopping
        /// </summary>
        public event EventHandler Stopping;

        /// <summary>
        /// Fired when progress changes
        /// </summary>
        public event EventHandler<ProgressChangedEventArgs> ProgressChanged;

        /// <summary>
        /// Event handler which fires after startup
        /// </summary>
        private EventHandler m_persistenceHandler;

        /// <summary>
        /// Install dataset
        /// </summary>
        public void InstallDataset(Dataset ds)
        {
            try
            {
                this.m_traceSource.TraceInfo("Applying {0} ({1} objects)...", ds.Id, ds.Action.Count);

                int i = 0;
                var bundle = new Bundle()
                {
                    Item = ds.Action.Where(o=>o.Element != null).Select(o =>
                    {
                        switch (o)
                        {
                            case DataUpdate du:
                                if (du.InsertIfNotExists)
                                    o.Element.BatchOperation = SanteDB.Core.Model.DataTypes.BatchOperationType.InsertOrUpdate;
                                else
                                    o.Element.BatchOperation = SanteDB.Core.Model.DataTypes.BatchOperationType.Update;
                                break;
                            case DataInsert di:
                                if (di.SkipIfExists)
                                    o.Element.BatchOperation = SanteDB.Core.Model.DataTypes.BatchOperationType.InsertOrUpdate;
                                else
                                    o.Element.BatchOperation = SanteDB.Core.Model.DataTypes.BatchOperationType.Insert;
                                break;
                            case DataDelete dd:
                                o.Element.BatchOperation = SanteDB.Core.Model.DataTypes.BatchOperationType.Delete;
                                break;
                        }
                        return o.Element;
                    }).ToList()
                };
                var bundleService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Bundle>>();
                if (bundleService is IReportProgressChanged progress)
                    progress.ProgressChanged += this.ProgressChanged;
                bundleService.Insert(bundle, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);
                if (bundleService is IReportProgressChanged progress2)
                    progress2.ProgressChanged -= this.ProgressChanged;

                // Execute the changes
                var isqlp = ApplicationServiceContext.Current.GetService<ISqlDataPersistenceService>();
                foreach (var de in ds.SqlExec.Where(o => o.InvariantName == isqlp?.InvariantName))
                {
                    this.m_traceSource.TraceInfo("Executing post-dataset SQL instructions for {0}...", ds.Id);
                    isqlp.ExecuteNonQuery(de.QueryText);
                }

                // Execute the service instructions
                foreach (var se in ds.ServiceExec)
                {
                    this.m_traceSource.TraceInfo("Executing post-dataset service instructions {0}.{1}()...", se.ServiceType, se.Method);
                    var serviceType = Type.GetType(se.ServiceType);
                    if (serviceType == null)
                    {
                        this.m_traceSource.TraceWarning("Cannot find service type {0}...", se.ServiceType);
                        continue;
                    }
                    var serviceInstance = ApplicationServiceContext.Current.GetService(serviceType);
                    if (serviceInstance == null)
                    {
                        this.m_traceSource.TraceWarning("Cannot find registered service for {0}...", serviceType);
                        continue;
                    }

                    // Invoke the method
                    var method = serviceType.GetRuntimeMethod(se.Method, se.Arguments.Select(o => o.GetType()).ToArray());
                    if (method == null)
                    {
                        this.m_traceSource.TraceWarning("Cannot find method {0} on service {1}...", se.Method, serviceType);
                        continue;
                    }

                    try
                    {
                        method.Invoke(serviceInstance, se.Arguments.ToArray());
                    }
                    catch (Exception e)
                    {
                        this.m_traceSource.TraceWarning("Could not execute post-service call {0} - {1}", se.ServiceType, e);
                    }

                }
                this.m_traceSource.TraceInfo("Applied {0} changes", ds.Action.Count);
            }
            catch (Exception e)
            {
                this.m_traceSource.TraceEvent(EventLevel.Error, "Error applying dataset {0}: {1}", ds.Id, e);
                throw;
            }
        }

        /// <summary>
        /// Ctor
        /// </summary>
        public DataInitializationService()
        {
            if (ApplicationServiceContext.Current.HostType == SanteDBHostType.Server)
                this.m_persistenceHandler = (o, e) =>
                {
                    this.InstallDataDirectory();
                };
        }

        /// <summary>
        /// Install data directory contents
        /// </summary>
        public void InstallDataDirectory(EventHandler<ProgressChangedEventArgs> fileProgress = null)
        {
            using (AuthenticationContext.EnterSystemContext())
            {
                try
                {
                    String dataDirectory = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "data");
                    this.m_traceSource.TraceEvent(EventLevel.Verbose, "Scanning Directory {0} for datasets", dataDirectory);

                    XmlSerializer xsz = XmlModelSerializerFactory.Current.CreateSerializer(typeof(Dataset));
                    var datasetFiles = Directory.GetFiles(dataDirectory, "*.dataset");
                    datasetFiles = datasetFiles.OrderBy(o => Path.GetFileName(o)).ToArray();
                    int i = 0;
                    // Perform migrations
                    foreach (var f in datasetFiles)
                    {
                        try
                        {
                            var logFile = Path.ChangeExtension(f, "completed");
                            if (File.Exists(logFile))
                                continue; // skip

                            using (var fs = File.OpenRead(f))
                            {
                                var ds = xsz.Deserialize(fs) as Dataset;
                                fileProgress?.Invoke(this, new ProgressChangedEventArgs(++i / (float)datasetFiles.Length, ds.Id));
                                this.m_traceSource.TraceEvent(EventLevel.Informational, "Installing {0}...", Path.GetFileName(f));
                                this.InstallDataset(ds);
                            }

                            File.Move(f, logFile);
                        }
                        catch (Exception ex)
                        {
                            this.m_traceSource.TraceEvent(EventLevel.Error, "Error applying {0}: {1}", f, ex);
                            throw;
                        }
                    }
                }
                finally
                {
                    this.m_traceSource.TraceEvent(EventLevel.Verbose, "Un-binding event handler");
                    ApplicationServiceContext.Current.Started -= this.m_persistenceHandler;
                }
            }
        }

        /// <summary>
        /// Start the service
        /// </summary>
        public bool Start()
        {
            this.m_traceSource.TraceInfo("Binding to startup...");
            ApplicationServiceContext.Current.Started += this.m_persistenceHandler;
            return true;
        }

        /// <summary>
        /// Stopped
        /// </summary>
        /// <returns></returns>
        public bool Stop()
        {
            return true;
        }
    }

#pragma warning enable CS0067
}