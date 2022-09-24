using HeyRed.Mime;
using SanteDB.Core.Security.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SanteDB.Authentication.OAuth2
{
    internal class LocalFolderAssetProvider : IAssetProvider
    {
        readonly string _BaseDirectory;
        private static readonly Regex s_BindingRegex = new Regex("{{\\s?\\$([A-Za-z0-9\\-_]*?)\\s?}}", RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(250));
        private static readonly string[] s_BindingMimeTypes = new[]
        {
            "text/html",
            "text/plain",
            "text/javascript",
            "text/css",
            "application/json",
            "application/javascript"
        };

        public LocalFolderAssetProvider(string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(baseDirectory))
            {
                throw new ArgumentNullException(nameof(baseDirectory));
            }

            _BaseDirectory = baseDirectory;

            if (!Directory.Exists(_BaseDirectory))
            {
                throw new ArgumentException("Directory does not exist.", nameof(baseDirectory));
            }
        }

        public (Stream content, string mimeType) GetAsset(string id, string locale, IDictionary<string, string> bindingValues)
        {
            if (string.IsNullOrEmpty(id))
            {
                id = "index.html";
            }

            //Calling Path.GetFullPath in case they try and be sneaky and add ../ to escape the base directory.
            string path = Path.GetFullPath(Path.Combine(_BaseDirectory, id));

            if (!path.StartsWith(_BaseDirectory))
            {
                throw new ArgumentException($"id {id} is invalid.", nameof(id));
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"File not found for id {id}.", path);
            }

            var extension = Path.GetExtension(path);
            var mimetype = MimeTypesMap.GetMimeType(extension);

            //Fast path for nothing to replace.
            if (null == bindingValues || bindingValues.Count == 0 || !s_BindingMimeTypes.Contains(mimetype))
            {
                return (new FileStream(path, FileMode.Open, FileAccess.Read), mimetype);
            }

            string content = File.ReadAllText(path);

            content = s_BindingRegex.Replace(content, m => bindingValues.TryGetValue(m.Groups[1].Value, out var str) ? str : m.ToString());

            var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));

            ms.Seek(0, SeekOrigin.Begin);

            return (ms, mimetype);
        }
    }
}
