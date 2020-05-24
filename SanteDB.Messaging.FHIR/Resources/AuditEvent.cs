/*
 * Based on OpenIZ - Based on OpenIZ, Copyright (C) 2015 - 2019 Mohawk College of Applied Arts and Technology
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
using SanteDB.Messaging.FHIR.Attributes;
using SanteDB.Messaging.FHIR.Backbone;
using SanteDB.Messaging.FHIR.DataTypes;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;

namespace SanteDB.Messaging.FHIR.Resources
{

    /// <summary>
    /// Audit event action
    /// </summary>
    [XmlType("AuditEventAction", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/audit-event-action")]
    public enum AuditEventAction
    {
        /// <summary>
        /// The action represents a create
        /// </summary>
        [XmlEnum("C")]
        Create,
        /// <summary>
        /// The action represents a read type transaction
        /// </summary>
        [XmlEnum("R")]
        Read,
        /// <summary>
        /// The action represents an update type transaction
        /// </summary>
        [XmlEnum("U")]
        Update,
        /// <summary>
        /// The action represents a delete type transaction
        /// </summary>
        [XmlEnum("D")]
        Delete,
        /// <summary>
        /// The action represents an execute type transaction
        /// </summary>
        [XmlEnum("E")]
        Execute
    }

    /// <summary>
    /// Audit event outcomes
    /// </summary>
    [XmlType("AuditEventOutcome", Namespace = "http://hl7.org/fhir")]
    [FhirValueSet(Uri = "http://hl7.org/fhir/ValueSet/audit-event-outcome")]
    public enum AuditEventOutcome
    {
        /// <summary>
        /// The event was successful
        /// </summary>
        [XmlEnum("0")]
        Success,
        /// <summary>
        /// The event resulted in a minor failure
        /// </summary>
        [XmlEnum("4")]
        MinorFail,
        /// <summary>
        /// The event resulted in a serious failure (i.e. process had to be stopped)
        /// </summary>
        [XmlEnum("8")]
        SeriousFail,
        /// <summary>
        /// The event resulted in a catastrophic failure
        /// </summary>
        [XmlEnum("12")]
        EpicFail
    }

    /// <summary>
    /// Represents an Audit event
    /// </summary>
    [XmlType("AuditEvent", Namespace = "http://hl7.org/fhir")]
    [XmlRoot("AuditEvent", Namespace = "http://hl7.org/fhir")]
    [Description("Represents an event record")]
    public class AuditEvent : DomainResourceBase
    {
        /// <summary>
        /// Represents an audit event
        /// </summary>
        public AuditEvent()
        {
            this.SubType = new List<FhirCoding>();
            this.Agent = new List<AuditEventAgent>();
            this.Entity = new List<AuditEventEntity>();
        }

        /// <summary>
        /// Represents the type of data
        /// </summary>
        [XmlElement("type")]
        [FhirElement(MinOccurs = 1)]
        [Description("Represents the type of audit event that occurred")]
        public FhirCoding Type { get; set; }

        /// <summary>
        /// Gets or sets the subtype coding
        /// </summary>
        [XmlElement("subtype")]
        [Description("Represents the sub-classification of the event")]
        public List<FhirCoding> SubType { get; set; }

        /// <summary>
        /// Represents the action code that occurred
        /// </summary>
        [XmlElement("action")]
        [Description("Represents the type of action performed")]
        public FhirCode<AuditEventAction> Action { get; set; }

        /// <summary>
        /// Gets or sets the recorded time
        /// </summary>
        [XmlElement("recorded")]
        [Description("Represents the instant in time that the event occurred")]
        [FhirElement(MinOccurs = 1)]
        public FhirInstant Recorded { get; set; }

        /// <summary>
        /// Gets or sets the outcome 
        /// </summary>
        [XmlElement("outcome")]
        [Description("Represents the outcome of the event")]
        public FhirCode<AuditEventOutcome> Outcome { get; set; }

        /// <summary>
        /// Gets or sets the outcome description
        /// </summary>
        [XmlElement("outcomeDesc")]
        [Description("A textual description of the outcome code")]
        public FhirString OutcomeDescription { get; set; }

        /// <summary>
        /// Gets or sets the agents in the transaction
        /// </summary>
        [XmlElement("agent")]
        [Description("Actors involved in the event")]
        [FhirElement(MinOccurs = 1)]
        public List<AuditEventAgent> Agent { get; set; }

        /// <summary>
        /// Gets or sets the source of the audit
        /// </summary>
        [XmlElement("source")]
        [Description("The source of the audit event information")]
        [FhirElement(MinOccurs = 1)]
        public AuditEventSource Source { get; set; }

        /// <summary>
        /// Gets or sets the entities involved in the event
        /// </summary>
        [XmlElement("entity")]
        [Description("The entities or objects involved in the event")]
        [FhirElement(MinOccurs = 1)]
        public List<AuditEventEntity> Entity { get; set; }
    }
}
