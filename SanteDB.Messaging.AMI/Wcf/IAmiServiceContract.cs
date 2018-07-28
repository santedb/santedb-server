using SanteDB.Core.Applets.Model;
using SanteDB.Core.Interop;
using SanteDB.Core.Model;
using SanteDB.Core.Model.AMI.Applet;
using SanteDB.Core.Model.AMI.Auth;
using SanteDB.Core.Model.AMI.Diagnostics;
using SanteDB.Core.Model.AMI.Logging;
using SanteDB.Core.Model.AMI.Security;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Model.Patch;
using SanteDB.Core.Model.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using SanteDB.Core.Alerting;
using SwaggerWcf.Attributes;
using SanteDB.Core.Model.AMI.Collections;
using SanteDB.Core.Model.Collection;

namespace SanteDB.Messaging.AMI.Wcf
{
    /// <summary>
    /// Represents a service contract for the AMI 
    /// </summary>
    [ServiceContract(ConfigurationName = "AMI_2.0", Name = "AMI"), XmlSerializerFormat]
    [ServiceKnownType(typeof(SecurityUserInfo))]
    [ServiceKnownType(typeof(Entity))]
    [ServiceKnownType(typeof(ExtensionType))]
    [ServiceKnownType(typeof(AlertMessage))]
    [ServiceKnownType(typeof(SecurityApplication))]
    [ServiceKnownType(typeof(TfaRequestInfo))]
    [ServiceKnownType(typeof(SecurityDeviceInfo))]
    [ServiceKnownType(typeof(SecurityApplicationInfo))]
    [ServiceKnownType(typeof(SecurityPolicyInfo))]
    [ServiceKnownType(typeof(SecurityRoleInfo))]
    [ServiceKnownType(typeof(AuditSubmission))]
    [ServiceKnownType(typeof(AuditInfo))]
    [ServiceKnownType(typeof(AppletManifest))]
    [ServiceKnownType(typeof(AppletManifestInfo))]
    [ServiceKnownType(typeof(DeviceEntity))]
    [ServiceKnownType(typeof(DiagnosticApplicationInfo))]
    [ServiceKnownType(typeof(DiagnosticAttachmentInfo))]
    [ServiceKnownType(typeof(DiagnosticBinaryAttachment))]
    [ServiceKnownType(typeof(DiagnosticTextAttachment))]
    [ServiceKnownType(typeof(DiagnosticEnvironmentInfo))]
    [ServiceKnownType(typeof(DiagnosticReport))]
    [ServiceKnownType(typeof(DiagnosticSyncInfo))]
    [ServiceKnownType(typeof(DiagnosticVersionInfo))]
    [ServiceKnownType(typeof(SubmissionInfo))]
    [ServiceKnownType(typeof(SubmissionResult))]
    [ServiceKnownType(typeof(ApplicationEntity))]
    [ServiceKnownType(typeof(SubmissionRequest))]
    [ServiceKnownType(typeof(ServiceOptions))]
    [ServiceKnownType(typeof(X509Certificate2Info))]
    [ServiceKnownType(typeof(CodeSystem))]
    [ServiceKnownType(typeof(LogFileInfo))]
    [ServiceKnownType(typeof(AmiCollection))]
    public interface IAmiServiceContract
    {
        /// <summary>
        /// Get the schema for this service
        /// </summary>
        [WebGet(UriTemplate = "/?xsd={schemaId}")]
        XmlSchema GetSchema(int schemaId);

        /// <summary>
        /// Creates the specified resource 
        /// </summary>
        /// <param name="resourceType">The type of resource to be created</param>
        /// <param name="data">The resource data to be created</param>
        /// <returns>The stored resource</returns>
        [WebInvoke(Method = "POST", UriTemplate = "/{resourceType}", BodyStyle = WebMessageBodyStyle.Bare)]
        Object Create(String resourceType, Object data);

        /// <summary>
        /// Creates the specified resource if it does not exist, otherwise updates it
        /// </summary>
        /// <param name="resourceType">The type of resource to be created</param>
        /// <param name="key">The key of the resource </param>
        /// <param name="data">The resource itself</param>
        /// <returns>The updated or created resource</returns>
        [WebInvoke(Method = "POST", UriTemplate = "/{resourceType}/{key}", BodyStyle = WebMessageBodyStyle.Bare)]
        Object CreateUpdate(String resourceType, String key, Object data);

        /// <summary>
        /// Updates the specified resource
        /// </summary>
        /// <param name="resourceType">The type of resource to be updated</param>
        /// <param name="key">The key of the resource</param>
        /// <param name="data">The resource data to be updated</param>
        /// <returns>The updated resource</returns>
        [WebInvoke(Method = "PUT", UriTemplate = "/{resourceType}/{key}", BodyStyle = WebMessageBodyStyle.Bare)]
        Object Update(String resourceType, String key, Object data);

        /// <summary>
        /// Deletes the specified resource
        /// </summary>
        /// <param name="resourceType">The type of resource being deleted</param>
        /// <param name="key">The key of the resource being deleted</param>
        /// <returns>The last version of the deleted resource</returns>
        [WebInvoke(Method = "DELETE", UriTemplate = "/{resourceType}/{key}", BodyStyle = WebMessageBodyStyle.Bare)]
        Object Delete(String resourceType, String key);

        /// <summary>
        /// Gets the specified resource from the service
        /// </summary>
        /// <param name="resourceType">The type of resource to be fetched</param>
        /// <param name="key">The key of the resource</param>
        /// <returns>The retrieved resource</returns>
        [WebInvoke(Method = "GET", UriTemplate = "/{resourceType}/{key}", BodyStyle = WebMessageBodyStyle.Bare)]
        Object Get(String resourceType, String key);

        /// <summary>
        /// Gets the specified versioned copy of the data
        /// </summary>
        /// <param name="resourceType">The type of resource</param>
        /// <param name="key">The key of the resource</param>
        /// <param name="versionKey">The version key to retrieve</param>
        /// <returns>The object as it existed at that version</returns>
        [WebInvoke(Method = "GET", UriTemplate = "/{resourceType}/{key}/history/{versionKey}", BodyStyle = WebMessageBodyStyle.Bare)]
        Object GetVersion(String resourceType, String key, String versionKey);

        /// <summary>
        /// Gets a complete history of changes made to the object (if supported)
        /// </summary>
        /// <param name="resourceType">The type of resource</param>
        /// <param name="key">The key of the object to retrieve the history for</param>
        /// <returns>The history</returns>
        [WebInvoke(Method = "GET", UriTemplate = "/{resourceType}/{key}/history", BodyStyle = WebMessageBodyStyle.Bare)]
        AmiCollection History(String resourceType, String key);

        /// <summary>
        /// Searches the specified resource type for matches
        /// </summary>
        /// <param name="resourceType">The resource type to be searched</param>
        /// <returns>The results of the search</returns>
        [WebInvoke(Method = "GET", UriTemplate = "/{resourceType}", BodyStyle = WebMessageBodyStyle.Bare)]
        AmiCollection Search(String resourceType);

        /// <summary>
        /// Get the service options
        /// </summary>
        /// <returns>The options of the server</returns>
        [WebInvoke(Method = "OPTIONS", UriTemplate = "/", BodyStyle = WebMessageBodyStyle.Bare)]
        ServiceOptions Options();

        /// <summary>
        /// Get the specific options supported for the 
        /// </summary>
        /// <param name="resourceType">The type of resource to get service options</param>
        [WebInvoke(Method = "OPTIONS", UriTemplate = "/{resourceType}", BodyStyle = WebMessageBodyStyle.Bare)]
        ServiceResourceOptions ResourceOptions(String resourceType);

        #region Diagnostic / Ad-Hoc interfaces

        /// <summary>
		/// Creates a diagnostic report.
		/// </summary>
		/// <param name="report">The diagnostic report to be created.</param>
		/// <returns>Returns the created diagnostic report.</returns>
		[WebInvoke(UriTemplate = "/sherlock", BodyStyle = WebMessageBodyStyle.Bare, Method = "POST")]
        [SwaggerWcfPath("Create Diagnostic Report", "Creates a diagnostic report. A diagnostic report contains logs and configuration information used to debug and resolve issues")]
        DiagnosticReport CreateDiagnosticReport(DiagnosticReport report);

        /// <summary>
		/// Gets a specific log file.
		/// </summary>
		/// <param name="logId">The log identifier.</param>
		/// <returns>Returns the log file information.</returns>
		[WebGet(UriTemplate = "/log/{logId}")]
        LogFileInfo GetLog(string logId);

        /// <summary>
        /// Get log files on the server and their sizes.
        /// </summary>
        /// <returns>Returns a collection of log files.</returns>
        [WebGet(UriTemplate = "/log")]
        AmiCollection GetLogs();

        /// <summary>
		/// Gets a server diagnostic report.
		/// </summary>
		/// <returns>Returns the created diagnostic report.</returns>
		[WebGet(UriTemplate = "/sherlock", BodyStyle = WebMessageBodyStyle.Bare)]
        [SwaggerWcfPath("Get Diagnostic Report", "A diagnostic report contains logs and configuration information used to debug and resolve issues")]
        DiagnosticReport GetServerDiagnosticReport();

        /// <summary>
		/// Ping the service to determine up/down
		/// </summary>
		[WebInvoke(UriTemplate = "/", Method = "PING")]
        [SwaggerWcfPath("Service Availability Status", "Forces the service to respond with a 204 if the AMI is running at this endpoint", ExternalDocsUrl = "http://santedb.org/artifacts/1.0/hdsi/", ExternalDocsDescription = "AMI Data Contract Documentation")]
        void Ping();

        #endregion

        #region Two-Factor Authentication

        /// <summary>
		/// Creates a request that the server issue a reset code
		/// </summary>
		[WebInvoke(UriTemplate = "/tfa", BodyStyle = WebMessageBodyStyle.Bare, Method = "POST")]
        void SendTfaSecret(TfaRequestInfo resetInfo);

        /// <summary>
        /// Gets the list of TFA mechanisms.
        /// </summary>
        /// <returns>Returns a list of TFA mechanisms.</returns>
        [WebGet(UriTemplate = "/tfa")]
        [SwaggerWcfPath("Get TFA Mechanism", "Retrieves a list of supported TFA mechanisms")]
        AmiCollection GetTfaMechanisms();

        #endregion

    }
}
