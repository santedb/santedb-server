﻿/*
 * Based on OpenIZ - Copyright 2015-2019 Mohawk College of Applied Arts and Technology
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
using SanteDB.Core.Attributes;
using SanteDB.Core.Configuration;
using System.Reflection;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("SanteDB.Caching.Memory")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Mohawk College of Applied Arts and Technology")]
[assembly: AssemblyProduct("SanteDB (http://SanteDB.org)")]
[assembly: AssemblyCopyright("Copyright (C) 2015-2018, Mohawk College of Applied Arts and Technology")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("8553abc7-abfa-4a21-b0e8-68b7de76129d")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("2.0.39.*")][assembly: AssemblyInformationalVersion("Langley")]
[assembly: AssemblyVersion("2.0.39.*")]
[assembly: AssemblyInformationalVersion("Langley")]
[assembly: AssemblyFileVersion("2.0.39.0")]

[assembly: Plugin(Group = FeatureGroup.Performance, Environment = PluginEnvironment.Server, EnableByDefault = true)]
[assembly: PluginDependency("SanteDB.Core, Version=1.10.0.0")]
[assembly: PluginTraceSource(SanteDB.Caching.Memory.MemoryCacheConstants.TraceSourceName)]
[assembly: PluginTraceSource(SanteDB.Caching.Memory.MemoryCacheConstants.QueryTraceSourceName)]
