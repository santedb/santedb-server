using SanteDB.BI.Model;
using SanteDB.BI.Services;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Services;
using SanteDB.Tools.Debug.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    [ServiceProvider("File Based BI Repository")]
    public class FileMetadataRepository : IBiMetadataRepository, IDaemonService
    {

        // Timer
        private Timer m_scanTimer;

        // Tracer for logs
        private Tracer m_tracer = Tracer.GetTracer(typeof(FileMetadataRepository));

        // Asset dictionary 
        private ConcurrentDictionary<String, String> m_assetDictionary = new ConcurrentDictionary<string, string>();

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
        /// Gets the BI definition from the specified file
        /// </summary>
        /// <typeparam name="TBisDefinition"></typeparam>
        /// <param name="id"></param>
        /// <returns></returns>
        public TBisDefinition Get<TBisDefinition>(string id) where TBisDefinition : BiDefinition
        {
            // Open the path to the specified asset
            string assetDefinitionPath = null;
            if (this.m_assetDictionary.TryGetValue(id, out assetDefinitionPath))
            {
                // Load and return
                try
                {
                    using (var fs = File.OpenRead(assetDefinitionPath))
                    {
                        return (TBisDefinition)BiDefinition.Load(fs);
                    }
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

            string path = this.m_configuration.BiMetadataRepository.BasePath;
            foreach (var s in metadata.Id.Split('.'))
                path = Path.Combine(path, s);
            path += ".xml";

            try
            {
                if(this.m_assetDictionary.ContainsKey(metadata.Id))
                {
                    this.m_tracer.TraceInfo("Removing existing definition for {0}", metadata.Id);
                    this.m_assetDictionary.TryRemove(metadata.Id, out path);
                }
                this.m_tracer.TraceInfo("Saving {0} to {1}", metadata.Id, path);
                using (var fs = File.Create(path))
                    metadata.Save(fs);
                this.m_assetDictionary.TryAdd(metadata.Id, path);
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
            int r = 0;
            var filterFn = filter.Compile();
            foreach (var f in this.m_assetDictionary.Values.Distinct())
            {
                using (var fs = File.OpenRead(f))
                {
                    var asset = BiDefinition.Load(fs);
                    if (asset.GetType().IsAssignableFrom(typeof(TBisDefinition)) && filterFn.Invoke((TBisDefinition)asset) && r++ > offset && r < count + offset)
                        yield return (TBisDefinition)asset;
                    else if (r > count + offset)
                        yield break;
                }
            }
        }

        /// <summary>
        /// Removes an object from the file store
        /// </summary>
        public void Remove<TBisDefinition>(string id) where TBisDefinition : BiDefinition
        {
            // Open the path to the specified asset
            string assetDefinitionPath = null;
            if (this.m_assetDictionary.TryGetValue(id, out assetDefinitionPath))
            {
                // Load and return
                try
                {
                    File.Move(assetDefinitionPath, Path.ChangeExtension(assetDefinitionPath, "bak"));
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

                this.LoadDefinitions(this.m_configuration.BiMetadataRepository.BasePath);

            }, null, 0, this.m_configuration.BiMetadataRepository.RescanTime);

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
            foreach(var f in Directory.GetFiles(path, "*.xml"))
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
                        foreach (var itm in package)
                            this.Insert(itm);
                        File.Move(f, Path.ChangeExtension(f, "bak"));
                    }
                    else
                        this.m_assetDictionary.TryAdd(asset.Id, f);

                }
                catch(Exception e)
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
    }
}
