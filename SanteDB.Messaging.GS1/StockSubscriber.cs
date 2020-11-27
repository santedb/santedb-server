/*
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
using SanteDB.Core.Services;
using SanteDB.Core;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Services;
using SanteDB.Messaging.GS1.Configuration;
using SanteDB.Messaging.GS1.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using SanteDB.Core.Diagnostics;

namespace SanteDB.Messaging.GS1
{
    /// <summary>
    /// Represents a notification service that listens to stock events and then prepares them for broadcast
    /// </summary>
    [ServiceProvider("GS1 Stock Event Subscriber", 
        Dependencies = new Type[] { typeof(IPersistentQueueService) })]
    public class StockSubscriber : IDaemonService
    {
        /// <summary>
        /// Gets the service name
        /// </summary>
        public string ServiceName => "GS1 Stock Events Subscription Service";

        /// <summary>
        /// True if running
        /// </summary>
        public bool IsRunning
        {
            get
            {
                return true;
            }
        }

        // Tracer
        private Tracer m_tracer = new Tracer(Gs1Constants.TraceSourceName);

        // GS1 utility
        private Gs1Util m_gs1Util;

        // Configuration
        private Gs1ConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<Gs1ConfigurationSection>();

        /// <summary>
        /// Daemon is started
        /// </summary>
        public event EventHandler Started;
        /// <summary>
        /// Daemon is starting
        /// </summary>
        public event EventHandler Starting;
        /// <summary>
        /// Daemon is stopped
        /// </summary>
        public event EventHandler Stopped;
        /// <summary>
        /// Daemon is stopping
        /// </summary>
        public event EventHandler Stopping;

        /// <summary>
        /// Start the daemon
        /// </summary>
        public bool Start()
        {
            this.Starting?.Invoke(this, EventArgs.Empty);

            ApplicationServiceContext.Current.Started += (o, e) =>
            {
                this.m_gs1Util = new Gs1Util();
                // Application has started let's bind to the events we need
                ApplicationServiceContext.Current.GetService<IDataPersistenceService<Act>>().Inserted += (xo, xe) => this.NotifyAct(new Act[] { xe.Data });
                ApplicationServiceContext.Current.GetService<IDataPersistenceService<Bundle>>().Inserted += (xo, xe) => this.NotifyAct(xe.Data.Item.OfType<Act>());

            };

            this.Started?.Invoke(this, EventArgs.Empty);
            return true;
        }

        /// <summary>
        /// Notify of created 
        /// </summary>
        private void NotifyAct(IEnumerable<Act> createdActs)
        {
            var acts = createdActs.Where(o => o.ClassConceptKey == ActClassKeys.Supply && o.Tags?.Any(t=>t.TagKey == "http://santedb.org/tags/contrib/importedData" && t.Value.ToLower(CultureInfo.InvariantCulture) == "true") == false).ToList(); // Get all supply events

            // Iterate over the supply acts and figure 
            foreach(var act in acts.Where(a => a != null))
            {
                if (act.MoodConceptKey == ActMoodKeys.Request) // We have an order!
                    this.IssueOrder(act);
                else if (act.MoodConceptKey == ActMoodKeys.Eventoccurrence && act.StatusConceptKey == StatusKeys.Completed &&
                        act?.Tags.FirstOrDefault(o => o.TagKey == "orderStatus")?.Value == "completed")
                    this.IssueReceiveAdvice(act);
            }
        }

        /// <summary>
        /// Issue receive advice
        /// </summary>
        private void IssueReceiveAdvice(Act act)
        {
            var receiveMessage = new ReceivingAdviceMessageType();
            receiveMessage.StandardBusinessDocumentHeader = this.m_gs1Util.CreateDocumentHeader("receivingAdvice", act.LoadCollection<ActParticipation>("Participations").FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.Authororiginator).LoadProperty<Entity>("PlayerEntity"));

            var originalOrder = act.LoadCollection<ActRelationship>("Relationships").FirstOrDefault(o => o.RelationshipTypeKey == ActRelationshipTypeKeys.Arrival)?.LoadProperty<Act>("TargetAct");

            Place shipTo = act.LoadCollection<ActParticipation>("Participations").FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.Location)?.LoadProperty<Place>("PlayerEntity"),
                shipFrom = originalOrder.LoadCollection<ActParticipation>("Participations").FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.Location)?.LoadProperty<Place>("PlayerEntity");

            // Receive message advice
            receiveMessage.receivingAdvice = new ReceivingAdviceType[]
            {
                new ReceivingAdviceType()
                {
                    creationDateTime = act.CreationTime.DateTime,
                    documentStatusCode = DocumentStatusEnumerationType.ORIGINAL,
                    receivingAdviceIdentification = new Ecom_EntityIdentificationType()
                    {
                        entityIdentification = act.Key.ToString()
                    },
                    receivingDateTime = act.ActTime.DateTime,
                    despatchAdvice = new Ecom_DocumentReferenceType()
                    {
                        entityIdentification = originalOrder.Identifiers.FirstOrDefault()?.Value ?? originalOrder.Key.Value.ToString(),
                        creationDateTime = originalOrder.ActTime.DateTime,
                        creationDateTimeSpecified = true,
                        contentOwner = new Ecom_PartyIdentificationType()
                        {
                            additionalPartyIdentification = originalOrder.Identifiers.Count > 0 ? new AdditionalPartyIdentificationType[]
                            {
                                new AdditionalPartyIdentificationType() { Value = originalOrder.Identifiers.FirstOrDefault()?.Authority.Oid, additionalPartyIdentificationTypeCode = "urn:oid:" },
                            } : null,
                        }
                    },
                    reportingCode = new GoodsReceiptReportingCodeType() { Value = "FULL_DETAILS" },
                    shipper = this.m_gs1Util.CreateLocation(shipFrom),
                    shipTo = this.m_gs1Util.CreateLocation(shipTo),
                    receiver = this.m_gs1Util.CreateLocation(shipTo),
                    receivingAdviceLogisticUnit = act.Participations.Where(o=>o.ParticipationRoleKey == ActParticipationKey.Consumable).Select(o=> this.m_gs1Util.CreateReceiveLineItem(o, originalOrder.Participations.FirstOrDefault(p=>p.PlayerEntityKey == o.PlayerEntityKey))).ToArray()
                }
            };

            for (int i = 0; i < receiveMessage.receivingAdvice[0].receivingAdviceLogisticUnit.Length; i++)
                receiveMessage.receivingAdvice[0].receivingAdviceLogisticUnit[i].receivingAdviceLineItem[0].lineItemNumber = (i + 1).ToString();

            
            // Queue The order on the file system
            this.QueueMessage(receiveMessage);
        }

        /// <summary>
        /// Issue order information
        /// </summary>
        private void IssueOrder(Act act)
        {

            var orderMessage = new OrderMessageType();

            orderMessage.StandardBusinessDocumentHeader = this.m_gs1Util.CreateDocumentHeader("order", act.LoadCollection<ActParticipation>("Participations").FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.Authororiginator).LoadProperty<Entity>("PlayerEntity"));

            var type = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>().GetConceptReferenceTerm(act.TypeConceptKey.Value, "GS1");

            Place shipTo = act.LoadCollection<ActParticipation>("Participations").FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.Location)?.LoadProperty<Place>("PlayerEntity"),
                shipFrom = act.LoadCollection<ActParticipation>("Participations").FirstOrDefault(o => o.ParticipationRoleKey == ActParticipationKey.Distributor)?.LoadProperty<Place>("PlayerEntity");

            OrderType order = new OrderType()
            {
                creationDateTime = act.CreationTime.DateTime,
                documentStatusCode = DocumentStatusEnumerationType.ORIGINAL,
                orderIdentification = new Ecom_EntityIdentificationType()
                {
                    entityIdentification = act.Key.Value.ToString()
                },
                orderTypeCode = new OrderTypeCodeType() { Value = type?.Mnemonic, codeListVersion = type?.LoadProperty<CodeSystem>("CodeSystem").Authority },
                isApplicationReceiptAcknowledgementRequired = true,
                isApplicationReceiptAcknowledgementRequiredSpecified = true,
                note = new Description500Type() { Value = act.LoadCollection<ActNote>("Notes").FirstOrDefault()?.Text },
                isOrderFreeOfExciseTaxDuty = false,
                isOrderFreeOfExciseTaxDutySpecified = true,
                orderLogisticalInformation = new OrderLogisticalInformationType()
                {
                    shipFrom = this.m_gs1Util.CreateLocation(shipFrom),
                    shipTo = this.m_gs1Util.CreateLocation(shipTo),
                    orderLogisticalDateInformation = new OrderLogisticalDateInformationType()
                    {
                        requestedDeliveryDateTime = new DateOptionalTimeType()
                        {
                            date = act.ActTime.Date
                        }
                    }
                },
                orderLineItem = act.LoadCollection<ActParticipation>("Participations").Where(o=>o.ParticipationRoleKey == ActParticipationKey.Product ).Select(o => this.m_gs1Util.CreateOrderLineItem(o)).ToArray()
            };

            for (int i = 0; i < order.orderLineItem.Length; i++)
                order.orderLineItem[i].lineItemNumber = (i + 1).ToString();

            orderMessage.order = new OrderType[] { order };

            // Queue The order on the file system
            this.QueueMessage(orderMessage);
        }

        /// <summary>
        /// Queue message
        /// </summary>
        private void QueueMessage(object orderMessage)
        {
            ApplicationServiceContext.Current.GetService<IPersistentQueueService>().Enqueue(this.m_configuration.Gs1QueueName, orderMessage);
        }

        /// <summary>
        /// Stopping
        /// </summary>
        public bool Stop()
        {
            this.Stopping?.Invoke(this, EventArgs.Empty);

            this.Stopped?.Invoke(this, EventArgs.Empty);
            return true;
        }
    }
}
