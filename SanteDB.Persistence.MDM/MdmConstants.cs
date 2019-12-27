/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
 *
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using System;

namespace SanteDB.Persistence.MDM
{
    /// <summary>
    /// Represents an MDM constant
    /// </summary>
    public static class MdmConstants
    {

        /// <summary>
        /// Relationship used to represents a local/master relationship
        /// </summary>
        /// <remarks>Whenever the MDM persistence layer is used the system will link incoming records (dirty records)
        /// with a generated pristine record tagged as a master record.</remarks>
        public static readonly Guid MasterRecordRelationship = Guid.Parse("97730a52-7e30-4dcd-94cd-fd532d111578");

        /// <summary>
        /// Relationship used to represent that a local record has a high probability of being a duplicate with a master record
        /// </summary>
        public static readonly Guid CandidateLocalRelationship = Guid.Parse("56cfb115-8207-4f89-b52e-d20dbad8f8cc");

        /// <summary>
        /// Represents a record of truth, this is a record which is promoted on the master record such that it is the "true" version of the record
        /// </summary>
        public static readonly Guid MasterRecordOfTruthRelationship = Guid.Parse("1C778948-2CB6-4696-BC04-4A6ECA140C20");

        /// <summary>
        /// Master record classification
        /// </summary>
        public static readonly Guid MasterRecordClassification = Guid.Parse("49328452-7e30-4dcd-94cd-fd532d111578");

        /// <summary>
        /// Determiner codes
        /// </summary>
        public static readonly Guid MasterRecordDeterminer = Guid.Parse("92837281-7e30-4dcd-94cd-fd532d111578");

        /// <summary>
        /// The name of the trace source to use for the MDM logs
        /// </summary>
        public const String TraceSourceName = "SanteDB.Persistence.MDM";

        /// <summary>
        /// MDM configuration name
        /// </summary>
        public const String ConfigurationSectionName = "santedb.mdm";
    }
}
