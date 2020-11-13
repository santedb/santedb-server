using SanteDB.Core.Configuration;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Notifications;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Linq;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Core.Services.Impl
{
    /// <summary>
    /// File notification template service
    /// </summary>
    [ServiceProvider("File System based Notification Template Repository", configurationType: typeof(FileSystemNotificationConfigurationSection))]
    public class FileNotificationTemplateRepository : INotificationTemplateRepository
    {

        // Tracer
        private Tracer m_tracer = Tracer.GetTracer(typeof(FileNotificationTemplateRepository));

        // Configuration
        private FileSystemNotificationConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<FileSystemNotificationConfigurationSection>();

        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "File System Bsaed Notification Template Repository";

        // Lock box
        private object m_lock = new object();

        // Repository
        private List<NotificationTemplate> m_repository = new List<NotificationTemplate>();

        /// <summary>
        /// Initialize 
        /// </summary>
        public FileNotificationTemplateRepository()
        {
            ApplicationServiceContext.Current.Started += (o, e) =>
            {
                try
                {
                    this.m_tracer.TraceInfo("Scanning {0}", this.m_configuration.RepositoryRoot);
                    if (!Directory.Exists(this.m_configuration.RepositoryRoot))
                        Directory.CreateDirectory(this.m_configuration.RepositoryRoot);

                    Directory.GetFiles(this.m_configuration.RepositoryRoot, "*.xml", SearchOption.AllDirectories)
                        .ToList().ForEach(f =>
                        {
                            try
                            {
                                lock (this.m_lock)
                                    using(var fs = File.OpenRead(f))
                                        this.m_repository.Add(NotificationTemplate.Load(fs));
                            }
                            catch (Exception ex)
                            {
                                this.m_tracer.TraceWarning("Skipping {0} - {1}", f, ex.Message);
                            }
                        });
                }
                catch (Exception ex)
                {
                    this.m_tracer.TraceError("Error loading templates - {0}", ex);
                }
            };
        }

        /// <summary>
        /// Find the specified templates
        /// </summary>
        public IEnumerable<NotificationTemplate> Find(Expression<Func<NotificationTemplate, bool>> filter)
        {
            lock(this.m_lock)
                return this.m_repository.Where(filter.Compile()).ToList();
        }

        /// <summary>
        /// Get the specified template
        /// </summary>
        public NotificationTemplate Get(string id, string lang)
        {
            lock(this.m_lock)
                return this.m_repository.FirstOrDefault(o => o.Id == id && o.Language == lang);
        }

        /// <summary>
        /// Insert the specified template
        /// </summary>
        public NotificationTemplate Insert(NotificationTemplate template)
        {
            if (this.Get(template.Id, template.Language) != null)
                throw new DuplicateKeyException($"Template {template.Id} for language {template.Language} already exists");

            return this.Update(template);
        }

        /// <summary>
        /// Update the specified template
        /// </summary>
        public NotificationTemplate Update(NotificationTemplate template)
        {
            try
            {
                lock(this.m_lock)
                {
                    this.m_repository.RemoveAll(o => o.Id == template.Id && o.Language == template.Language);
                    this.m_repository.Add(template);
                }

                var fileName = Path.Combine(this.m_configuration.RepositoryRoot, template.Language ?? "default", template.Id + ".xml");
                if (!Directory.Exists(Path.GetDirectoryName(fileName)))
                    Directory.CreateDirectory(Path.GetDirectoryName(fileName));

                using (var fs = File.Create(fileName))
                    return template.Save(fs);
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error updating the specified template - {0}", e);
                throw new Exception($"Error inserting {template.Id}", e);
            }
        }
    }
}
