using SanteDB.Core.Attributes;
using SanteDB.Core.Configuration;
using SanteDB.Messaging.Metadata;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("SanteDB.Messaging.Metadata")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("SanteDB.Messaging.Metadata")]
[assembly: AssemblyCopyright("Copyright ©  2019")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("1b690052-ed2e-4389-838d-9b9fb188f541")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.79.0.*")]
[assembly: AssemblyFileVersion("1.79.0.0")]

[assembly: Plugin(EnableByDefault = false, Environment = PluginEnvironment.Server, Group = FeatureGroup.Development)]
[assembly: PluginDependency("SanteDB.Core, Version=1.79.0.0")]
[assembly: PluginDependency("RestSrvr, Version=1.79.0.0")]
[assembly: PluginTraceSource(MetadataConstants.TraceSourceName)]