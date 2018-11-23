using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SanteDB.Messaging.FHIR.DataTypes;
using System.Xml.Serialization;
using System.Net;
using System.Diagnostics;

namespace SanteDB.Messaging.FHIR.Resources
{

    /// <summary>
    /// Resource 
    /// </summary>
    [XmlType("Reference", Namespace  = "http://hl7.org/fhir")]
    public class Reference : FhirElement
    {


        /// <summary>
        /// Gets or sets the reference
        /// </summary>
        [XmlElement("reference")]
        public FhirString ReferenceUrl { get; set; }

        /// <summary>
        /// Gets or sets the display
        /// </summary>
        [XmlElement("display")]
        public FhirString Display { get; set; }

        /// <summary>
        /// Create resource refernece (friendly method)
        /// </summary>
        public static Reference CreateResourceReference(DomainResourceBase instance, Uri baseUri)
        {
            return new Reference()
            {
                Display = instance.ToString(),
                //Type = new PrimitiveCode<string>(instance.GetType().GetCustomAttribute<XmlRootAttribute>() != null ? instance.GetType().GetCustomAttribute<XmlRootAttribute>().ElementName : instance.GetType().Name),
                ReferenceUrl = String.IsNullOrEmpty(instance.VersionId) ?
                    baseUri.ToString() + String.Format("/{0}/{1}", instance.GetType().Name, instance.Id) :
                    baseUri.ToString() + String.Format("/{0}/{1}/_history/{2}", instance.GetType().Name, instance.Id, instance.VersionId)
            };
        }

        /// <summary>
        /// Create resource refernece for local
        /// </summary>
        public static Reference CreateLocalResourceReference(DomainResourceBase instance)
        {
            IdRef idRef = instance.MakeIdRef();
            return new Reference() {
                ReferenceUrl = idRef.Value,
                Display = instance.ToString()
            };
        }

        /// <summary>
        /// Write text
        /// </summary>
        internal override void WriteText(System.Xml.XmlWriter w)
        {
            w.WriteStartElement("a");
            w.WriteAttributeString("href", this.ReferenceUrl.Value);
            w.WriteString((this.Display ?? this.ReferenceUrl).Value);
            w.WriteEndElement();// a
        }

        internal static Reference<StructureDefinition> CreateResourceReference(StructureDefinition structureDef, object baseUri)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Fetch the resource described by this item
        /// </summary>
        public DomainResourceBase FetchResource(Uri baseUri, Type resourceType)
        {
            return this.FetchResource(baseUri, null, null, resourceType);
        }

        /// <summary>
        /// Fetch a resource from the specified uri with the specified credentials
        /// </summary>
        public DomainResourceBase FetchResource(Uri baseUri, ICredentials credentials, DomainResourceBase context, Type resourceType)
        {
            // Request uri
            Uri requestUri = null;

            // Contained?
            if (this.ReferenceUrl.Value.StartsWith("#") && context.Contained != null)
            {
                var res = context.Contained.Find(o => o.Item.Id == this.ReferenceUrl.Value.ToString().Substring(1));
                if (res != null)
                    return res.Item;
            }
            else if (Uri.TryCreate(this.ReferenceUrl.Value, UriKind.RelativeOrAbsolute, out requestUri))
                requestUri = new Uri(baseUri, this.ReferenceUrl.Value.ToString());

            // Make request to URI
            Trace.TraceInformation("Fetching from {0}...", requestUri);
            var webReq = HttpWebRequest.Create(requestUri);
            webReq.Method = "GET";
            webReq.Credentials = credentials;

            // Fetch
            try
            {
                using (var response = webReq.GetResponse() as HttpWebResponse)
                {
                    if (response.StatusCode != HttpStatusCode.Accepted)
                        throw new WebException(String.Format("Server responded with {0}", response.StatusCode));

                    XmlSerializer xsz = new XmlSerializer(resourceType);
                    return xsz.Deserialize(response.GetResponseStream()) as DomainResourceBase;
                }
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                throw;
            }

        }

        /// <summary>
        /// Create a reasource reference to the specified resource
        /// </summary>
        public static Reference<TResource> CreateResourceReference<TResource>(TResource instance, Uri baseUri) where TResource : DomainResourceBase
        {
            return new Reference<TResource>()
            {
                Display = instance.ToString(),
                ReferenceUrl = String.IsNullOrEmpty(instance.VersionId) ?
                    baseUri.ToString() + String.Format("/{0}/{1}", instance.GetType().Name, instance.Id) :
                    baseUri.ToString() + String.Format("/{0}/{1}/_history/{2}", instance.GetType().Name, instance.Id, instance.VersionId)
            };
        }

        /// <summary>
        /// Create a reasource reference to the specified resource
        /// </summary>
        public static Reference<TResource> CreateLocalResourceReference<TResource>(TResource instance) where TResource : DomainResourceBase
        {
            IdRef idRef = instance.MakeIdRef();
            return new Reference<TResource>()
            {
                Display = instance.ToString(),
                ReferenceUrl = idRef.Value
            };
        }
    }

    /// <summary>
    /// Identifies a resource link
    /// </summary>
    public class Reference<TResource> : Reference
        where TResource : DomainResourceBase
    {

        /// <summary>
        /// Fetch the resource described by this item
        /// </summary>
        public TResource FetchResource(Uri baseUri, DomainResourceBase context)
        {
            return this.FetchResource(baseUri, null, context);
        }

        /// <summary>
        /// Fetch a resource from the specified uri with the specified credentials
        /// </summary>
        public TResource FetchResource(Uri baseUri, ICredentials credentials, DomainResourceBase context) 
        {
            return (TResource)base.FetchResource(baseUri, credentials, context, typeof(TResource));
        }

        /// <summary>
        /// Gets or sets the type
        /// </summary>
        [XmlIgnore]
        public FhirCode<String> Type 
        {
            get
            {
                Object[] atts = typeof(TResource).GetCustomAttributes(typeof(XmlRootAttribute), true);
                if (atts.Length == 1)
                    return new FhirCode<String>((atts[0] as XmlRootAttribute).ElementName);
                return new FhirCode<string>(typeof(TResource).Name);
            }
        }

    }
}
