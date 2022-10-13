using SanteDB.Core.Applets;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SanteDB.Authentication.OAuth2
{
    internal class AppletAssetProvider : IAssetProvider
    {
        readonly ReadonlyAppletCollection _Applets;
        readonly string _LoginAssetPath;

        public AppletAssetProvider(ReadonlyAppletCollection applets)
        {
            _Applets = applets;

            foreach (var applet in _Applets)
            {
                if (null == applet)
                {
                    continue;
                }

                _LoginAssetPath = applet.Settings?.FirstOrDefault(setting => setting.Name == "oauth2.login")?.Value ?? _LoginAssetPath;

                if (null != _LoginAssetPath)
                {
                    break;
                }
            }

            if (!_LoginAssetPath.EndsWith("/"))
            {
                _LoginAssetPath = $"{_LoginAssetPath}/";
            }

        }

        public (Stream content, string mimeType) GetAsset(string id, string locale, IDictionary<string, string> bindingValues)
        {
            if (string.IsNullOrEmpty(id))
            {
                id = "index.html";
            }
            else if (id.StartsWith("/ ")) //Remove a leading slash.
            {
                id = id.Substring(1);
            }

            var assetpath = _LoginAssetPath + id;

            var asset = _Applets.ResolveAsset(assetpath);

            if (null != asset)
            {
                var stream = new MemoryStream(_Applets.RenderAssetContent(asset, locale ?? CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, allowCache: false, bindingParameters: bindingValues));
                return (stream, asset.MimeType);
            }
            else
            {
                throw new FileNotFoundException("Asset not found", assetpath);
            }

        }
    }
}
