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
    [ServiceProvider("Dataset Installation Service")]
    #pragma warning disable CS0067
    public class DataInitializationService : IDaemonService, IReportProgressChanged
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "DataSet Initialization Service";

        // Trace source
        private Tracer m_traceSource = new Tracer(SanteDBConstants.DatasetInstallSourceName);

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
                // Can this dataset be installed as a bundle?
                if(ds.Action.All(o=>o is DataUpdate && (o as DataUpdate).InsertIfNotExists && !(o.Element is IVersionedAssociation)) && ds.Action.Count < 1000)
                {
                    this.m_traceSource.TraceVerbose("Will install as a bundle");
                    var bundle = new Bundle()
                    {
                        Item = ds.Action.Select(o => o.Element).ToList()
                    };

                    var progress = ApplicationServiceContext.Current.GetService<IDataPersistenceService<Bundle>>() as IReportProgressChanged;
                    if (progress != null)
                        progress.ProgressChanged += this.ProgressChanged;
                    ApplicationServiceContext.Current.GetService<IDataPersistenceService<Bundle>>().Insert(bundle, TransactionMode.Commit, AuthenticationContext.SystemPrincipal);
                    if (progress != null)
                        progress.ProgressChanged -= this.ProgressChanged;
                }
                else 
                    foreach (var itm in ds.Action.Where(o=>o.Element != null))
                    {
                        try
                        {
                            this.ProgressChanged?.Invoke(this, new ProgressChangedEventArgs(i++ / (float)ds.Action.Count, ds.Id));
                            //if (ApplicationServiceContext.Current.GetService<IDataCachingService>()?.Size > 10000) // Probably a good idea to clear memcache
                            //    ApplicationServiceContext.Current.GetService<IDataCachingService>().Clear();

                            // IDP Type
                            Type idpType = typeof(IDataPersistenceService<>);
                            idpType = idpType.MakeGenericType(new Type[] { itm.Element.GetType() });
                            var idpInstance = ApplicationServiceContext.Current.GetService(idpType) as IDataPersistenceService;

                            // Don't insert duplicates
                            var getMethod = idpType.GetMethod("Get");

                            this.m_traceSource.TraceEvent(EventLevel.Verbose, "{0} {1}", itm.ActionName, itm.Element);

                            Object target = null, existing = null;
                            if (itm.Element.Key.HasValue)
                            {
                                ApplicationServiceContext.Current.GetService<IDataCachingService>()?.Remove(itm.Element.Key.Value);
                                existing = idpInstance.Get(itm.Element.Key.Value);
                            }
                            if (existing != null)
                            {
                                if ((itm as DataInsert)?.SkipIfExists == true) continue;
                                target = (existing as IdentifiedData).Clone();
                                target.CopyObjectData(itm.Element);
                            }
                            else
                                target = itm.Element;

                            // Association
                            if (itm.Association != null)
                                foreach (var ascn in itm.Association)
                                {
                                    var pi = target.GetType().GetRuntimeProperty(ascn.PropertyName);
                                    var mi = pi.PropertyType.GetRuntimeMethod("Add", new Type[] { ascn.Element.GetType() });
                                    mi.Invoke(pi.GetValue(target), new object[] { ascn.Element });
                                }

                            // ensure version sequence is set
                            // the reason this exists is because in FHIR code system values have the same mnemonic values in different code systems
                            // since we cannot insert duplicate mnemonic values, we want to associate the existing concept with the new reference term
                            // for the new code system
                            //
                            // i.e.
                            //
                            // Code System Address Use has the following codes:
                            // home
                            // work
                            // temp
                            // old
                            // we want to insert reference terms and concepts so we can find an associated concept
                            // for a given reference term and code system
                            // 
                            // Code System Contact Point Use has the following codes:
                            // home
                            // work
                            // temp
                            // old
                            // mobile
                            //
                            // we can insert new reference terms for these reference terms, but cannot insert new concept using the same values for the mnemonic
                            // so we associate the new reference term and the concept
                            if (target is IVersionedAssociation)
                            {
                                var ivr = target as IVersionedAssociation;

                                // Get the type this is bound to
                                Type stype = target.GetType();
                                while (!stype.IsGenericType || stype.GetGenericTypeDefinition() != typeof(VersionedAssociation<>))
                                    stype = stype.BaseType;

                                ApplicationServiceContext.Current.GetService<IDataCachingService>()?.Remove(ivr.SourceEntityKey.Value);
                                var idt = typeof(IDataPersistenceService<>).MakeGenericType(stype.GetGenericArguments()[0]);
                                var idp = ApplicationServiceContext.Current.GetService(idt) as IDataPersistenceService;
                                ivr.EffectiveVersionSequenceId = (idp.Get(ivr.SourceEntityKey.Value) as IVersionedEntity)?.VersionSequence;
                                if (ivr.EffectiveVersionSequenceId == null)
                                    throw new KeyNotFoundException($"Dataset contains a reference to an unkown source entity : {ivr.SourceEntityKey}");
                                target = ivr;
                            }

                            if (existing == null && (itm is DataInsert || (itm is DataUpdate && (itm as DataUpdate).InsertIfNotExists)))
                                idpInstance.Insert(target);
                            else if (!(itm is DataInsert))
                                typeof(IDataPersistenceService).GetMethod(itm.ActionName, new Type[] { typeof(Object) }).Invoke(idpInstance, new object[] { target });
                            else if (existing != null && itm is DataObsolete)
                                idpInstance.Obsolete(existing);
                        }
                        catch(Exception e)
                        {
                            this.m_traceSource.TraceEvent(EventLevel.Verbose, "There was an issue in the dataset file {0} : {1} @ {2} ", ds.Id, e, itm.Element);
                            if (!itm.IgnoreErrors)
                                throw;
                        }
                    }

                // Execute the changes
                var isqlp = ApplicationServiceContext.Current.GetService<ISqlDataPersistenceService>();
                foreach (var de in ds.Exec.Where(o => o.InvariantName == isqlp?.InvariantName))
                {
                    this.m_traceSource.TraceInfo("Executing post-dataset SQL instructions for {0}...", ds.Id);
                    isqlp.ExecuteNonQuery(de.QueryText);
                }
                this.m_traceSource.TraceInfo("Applied {0} changes", ds.Action.Count);

            }
            catch(Exception e)
            {
                this.m_traceSource.TraceEvent(EventLevel.Error,  "Error applying dataset {0}: {1}", ds.Id, e);
                throw;
            }
        }

        /// <summary>
        /// Ctor
        /// </summary>
        public DataInitializationService()
        {
            if(ApplicationServiceContext.Current.HostType == SanteDBHostType.Server)
                this.m_persistenceHandler = (o, e) => {

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
