/*
 * Copyright (C) 2021 - 2021, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
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
 * Date: 2021-8-27
 */
using SanteDB.Core.Model;
using SanteDB.BI.Model;
using SanteDB.BI.Services;
using SanteDB.BI.Util;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Model.Query;
using SanteDB.Core.Services;
using SanteDB.Tools.Debug.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SanteDB.Tools.Debug.BI
{
    /// <summary>
    /// Represents a BI repository service which uses the file system
    /// </summary>
    [ExcludeFromCodeCoverage]
    [ServiceProvider("File Based BI Repository")]
    public class FileMetadataRepository : IBiMetadataRepository, IDaemonService
    {

        /// <summary>
        /// Metadata structure holding the file metadata
        /// </summary>
        private struct FileBiMetadata
        {

            /// <summary>
            /// File BI metadata
            /// </summary>
            public FileBiMetadata(BiDefinition definition, String filePath)
            {
                this.Definition = definition;
                this.FilePath = filePath;
            }

            /// <summary>
            /// Gets the definition
            /// </summary>
            public BiDefinition Definition { get; private set; }

            /// <summary>
            /// Gets the path to the file
            /// </summary>
            public String FilePath { get; private set; }

        }

        // Timer
        private Timer m_scanTimer;

        // Tracer for logs
        private readonly Tracer m_tracer = Tracer.GetTracer(typeof(FileMetadataRepository));

        // Asset dictionary
        private ConcurrentDictionary<String, FileBiMetadata> m_assetDictionary = new ConcurrentDictionary<string, FileBiMetadata>();

        // Configuration
        private DebugToolsConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<DebugToolsConfigurationSection>();

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "File-based BI Repository";

        /// <summary>
        /// True if the service is running
        /// </summary>
        public bool IsRunning => this.m_scanTimer != null;

        /// <summary>
        /// This is a local repository
        /// </summary>
        public bool IsLocal => true;

        /// <summary>
        /// Service is starting
        /// </summary>
        public event EventHandler Starting;

        /// <summary>
        /// Service is started
        /// </summary>
        public event EventHandler Started;

        /// <summary>
        /// Service is stopping
        /// </summary>
        public event EventHandler Stopping;

        /// <summary>
        /// Service is stopped
        /// </summary>
        public event EventHandler Stopped;

        /// <summary>
        /// Constructor for the file metadata repository
        /// </summary>
        public FileMetadataRepository()
        {
            // Validate paths
            for (var i = this.m_configuration.BiMetadataRepository.Count - 1; i >= 0; i--)
            {
                var dir = this.m_configuration.BiMetadataRepository[i].Path;
                if (!Directory.Exists(dir))
                {
                    this.m_tracer.TraceWarning("Cannot find directory {0}, removing from configuration", dir);
                    this.m_configuration.BiMetadataRepository.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Gets the BI definition from the specified file
        /// </summary>
        /// <typeparam name="TBisDefinition"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public TBisDefinition Get<TBisDefinition>(string id) where TBisDefinition : BiDefinition
        {
            // Open the path to the specified asset
            if (this.m_assetDictionary.TryGetValue(id, out var assetDefinition))
            {
                // Load and return
                try
                {
                    using (var fs = File.OpenRead(assetDefinition.FilePath))
                    {
                        return (TBisDefinition)BiDefinition.Load(fs);
                    }
                }
                catch (InvalidCastException)
                {
                    return null;
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error getting BIS definition {0} : {1}", id, e);
                    throw new Exception($"Error getting BIS definition {id}", e);
                }
            }
            else
                throw new KeyNotFoundException($"Cannot find definition for {id}");
        }

        /// <summary>
        /// Insert a record into the file system storage
        /// </summary>
        /// <typeparam name="TBisDefinition">The type of definiton being inserted</typeparam>
        /// <param name="metadata">The metadata parameters being saved</param>
        /// <returns>The stored metadata parameter</returns>
        public TBisDefinition Insert<TBisDefinition>(TBisDefinition metadata) where TBisDefinition : BiDefinition
        {

            string path = this.m_configuration.BiMetadataRepository.First().Path;
            foreach (var s in metadata.Id.Split('.'))
                path = Path.Combine(path, s);
            path += ".xml";

            try
            {
                if (this.m_assetDictionary.ContainsKey(metadata.Id))
                {
                    this.m_tracer.TraceInfo("Removing existing definition for {0}", metadata.Id);
                    this.m_assetDictionary.TryRemove(metadata.Id, out _);
                }
                this.m_tracer.TraceInfo("Saving {0} to {1}", metadata.Id, path);

                if (!Directory.Exists(Path.GetDirectoryName(path)))
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                using (var fs = File.Create(path))
                    metadata.Save(fs);
                this.m_assetDictionary.TryAdd(metadata.Id, new FileBiMetadata(metadata, path));
                return metadata;
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error inserting meta-data file {0} : {1}", path, e);
                throw new Exception($"Error inserting meta-data file {path}", e);
            }
        }

        /// <summary>
        /// Query for the specified set of data
        /// </summary>
        /// <typeparam name="TBisDefinition">The type of definition to query for</typeparam>
        /// <param name="filter">The filter for the query</param>
        /// <param name="offset">The offset of the first record</param>
        /// <param name="count">The number of records to return</param>
        /// <returns>The matching records</returns>
        public IEnumerable<TBisDefinition> Query<TBisDefinition>(Expression<Func<TBisDefinition, bool>> filter, int offset, int? count) where TBisDefinition : BiDefinition
        {
            return this.Query<TBisDefinition>(filter).Skip(offset).Take(count ?? 100);
        }

        /// <summary>
        /// Removes an object from the file store
        /// </summary>
        public void Remove<TBisDefinition>(string id) where TBisDefinition : BiDefinition
        {
            // Open the path to the specified asset
            if (this.m_assetDictionary.TryGetValue(id, out var assetDefinition))
            {
                // Load and return
                try
                {
                    File.Move(assetDefinition.FilePath, Path.ChangeExtension(assetDefinition.FilePath, "bak"));
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error getting BIS definition {0} : {1}", id, e);
                    throw new Exception($"Error getting BIS definition {id}", e);
                }
            }
            else
                throw new KeyNotFoundException($"Cannot find asset with id {id}");
        }

        /// <summary>
        /// Starts the file service monitoring for new assets which are placed on the file system
        /// </summary>
        public bool Start()
        {
            this.m_tracer.TraceInfo("Starting File-Based Asset Definition Repository...");
            this.Starting?.Invoke(this, EventArgs.Empty);

            this.m_scanTimer = new Timer(_ =>
            {
                foreach (var path in this.m_configuration.BiMetadataRepository)
                    this.LoadDefinitions(path.Path);
            }, null, 0, this.m_configuration.BiMetadataRepository.First().RescanTime);

            this.Started?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Load definitions
        /// </summary>
        private void LoadDefinitions(string path)
        {
            this.m_tracer.TraceInfo("Scanning {0} for definitions...", path);

            foreach (var subDir in Directory.GetDirectories(path))
                this.LoadDefinitions(subDir);
            foreach (var f in Directory.GetFiles(path, "*.xml"))
            {
                try
                {
                    BiDefinition asset = null;
                    using (var fs = File.OpenRead(f))
                        asset = BiDefinition.Load(fs);

                    if (asset is BiPackage)
                    {
                        this.m_tracer.TraceInfo("Expanding package {0}", asset.Id);
                        var package = asset as BiPackage;
                        foreach (var itm in new BiPackageEnumerator(package))
                        {
                            itm.MetaData = itm.MetaData ?? package.MetaData;
                            this.Insert(itm);
                        }
                        File.Move(f, Path.ChangeExtension(f, "bak"));
                    }
                    else
                        this.m_assetDictionary.TryAdd(asset.Id, new FileBiMetadata(asset, f));
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceWarning("File {0} could not be loaded : {1}", f, e);
                }
            }
        }

        /// <summary>
        /// Stops the service
        /// </summary>
        public bool Stop()
        {
            this.Stopping?.Invoke(this, EventArgs.Empty);

            this.m_scanTimer?.Dispose();

            this.Stopped?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <inheritdoc/>
        public IQueryResultSet<TBisDefinition> Query<TBisDefinition>(Expression<Func<TBisDefinition, bool>> filter) where TBisDefinition : BiDefinition
        {
            int r = 0;
            var filterFn = filter.Compile();
            return new TransformQueryResultSet<FileBiMetadata, TBisDefinition>(
                this.m_assetDictionary.Values.Where(p=>p.Definition is TBisDefinition).Distinct().AsResultSet(),
                f =>
                {
                    using (var fs = File.OpenRead(f.FilePath))
                    {
                        // We always load to get the most recent version
                        var asset = BiDefinition.Load(fs);
                        return asset as TBisDefinition;
                    }
                }
            );
        }
    }
}