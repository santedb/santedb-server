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
using RestSrvr.Attributes;
using SanteDB.Core;
using SanteDB.Core.Diagnostics;
using SanteDB.Core.Extensions;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Security;
using SanteDB.Core.Services;
using SanteDB.Messaging.GS1.Configuration;
using SanteDB.Messaging.GS1.Model;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SanteDB.Messaging.GS1.Rest
{
    /// <summary>
    /// GS1 BMS 3.3
    /// </summary>
    /// <remarks>The SanteDB server implementation of the GS1 BMS 3.3 interface over REST</remarks>
    [ServiceBehavior(Name = "GS1BMS", InstanceMode = ServiceInstanceMode.PerCall)]
    public class StockServiceBehavior : IStockService
    {

        // Configuration
        private Gs1ConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<Gs1ConfigurationSection>();

        // Act repository
        private IRepositoryService<Act> m_actRepository;
        // Material repository
        private IRepositoryService<Material> m_materialRepository;
        // Manufactured materials
        private IRepositoryService<ManufacturedMaterial> m_manufMaterialRepository;
        // Place repository
        private IRepositoryService<Place> m_placeRepository;
        // Stock service
        private IStockManagementRepositoryService m_stockService;
        // GS1 Utility
        private Gs1Util m_gs1Util;

        // Tracer
        private Tracer m_tracer = new Tracer(Gs1Constants.TraceSourceName);

        /// <summary>
        /// Default ctor setting services
        /// </summary>
        public StockServiceBehavior()
        {
            this.m_actRepository = ApplicationServiceContext.Current.GetService<IRepositoryService<Act>>();
            this.m_materialRepository = ApplicationServiceContext.Current.GetService<IRepositoryService<Material>>();
            this.m_placeRepository = ApplicationServiceContext.Current.GetService<IRepositoryService<Place>>();
            this.m_stockService = ApplicationServiceContext.Current.GetService<IStockManagementRepositoryService>();
            this.m_manufMaterialRepository = ApplicationServiceContext.Current.GetService<IRepositoryService<ManufacturedMaterial>>();
            this.m_gs1Util = new Gs1Util();
        }

        // HDSI Trace host
        private readonly Tracer traceSource = new Tracer(Gs1Constants.TraceSourceName);

        /// <summary>
        /// The issue despactch advice message will insert a new shipped order into the TImR system.
        /// </summary>
        public void IssueDespatchAdvice(DespatchAdviceMessageType advice)
        {
            if (advice == null || advice.despatchAdvice == null)
                throw new InvalidOperationException("Invalid message sent");
            // TODO: Validate the standard header
            // Loop 
            Bundle orderTransaction = new Bundle();

            foreach (var adv in advice.despatchAdvice)
            {

                // Has this already been created?
                Place sourceLocation = this.m_gs1Util.GetLocation(adv.shipper),
                    destinationLocation = this.m_gs1Util.GetLocation(adv.receiver);
                if (sourceLocation == null)
                    throw new KeyNotFoundException($"Shipper location not found");
                else if (destinationLocation == null)
                    throw new KeyNotFoundException($"Receiver location not found");

                // Find the original order which this despatch advice is fulfilling
                Act orderRequestAct = null;
                if (adv.orderResponse != null || adv.purchaseOrder != null)
                {
                    orderRequestAct = this.m_gs1Util.GetOrder(adv.orderResponse ?? adv.purchaseOrder, ActMoodKeys.Request);
                    if (orderRequestAct != null) // Orderless despatch!
                    {
                        // If the original order request is not comlete, then complete it
                        orderRequestAct.StatusConceptKey = StatusKeys.Completed;
                        orderTransaction.Add(orderRequestAct);
                    }
                }

                // Find the author of the shipment

                var oidService = ApplicationServiceContext.Current.GetService<IAssigningAuthorityRepositoryService>();
                var gln = oidService.Get("GLN");
                AssigningAuthority issuingAuthority = null;
                if(adv.despatchAdviceIdentification.contentOwner != null)
                    issuingAuthority = oidService.Find(o=>o.Oid == $"{gln.Oid}.{adv.despatchAdviceIdentification.contentOwner.gln}").FirstOrDefault();
                if (issuingAuthority == null)
                    issuingAuthority = oidService.Get(this.m_configuration.DefaultContentOwnerAssigningAuthority);

                if (issuingAuthority == null)
                    throw new KeyNotFoundException("Cannot find default issuing authority for advice identification. Please configure a valid OID");

                int tr = 0;
                var existing = this.m_actRepository.Find(o => o.Identifiers.Any(i => i.AuthorityKey == issuingAuthority.Key && i.Value == adv.despatchAdviceIdentification.entityIdentification), 0, 1, out tr);
                if (existing.Any())
                {
                    this.m_tracer.TraceWarning("Duplicate despatch {0} will be ignored", adv.despatchAdviceIdentification.entityIdentification);
                    continue;
                }

                // Now we want to create a new Supply act which that fulfills the old act
                Act fulfillAct = new Act()
                {
                    CreationTime = DateTimeOffset.Now,
                    MoodConceptKey = ActMoodKeys.Eventoccurrence,
                    ClassConceptKey = ActClassKeys.Supply,
                    StatusConceptKey = StatusKeys.Active,
                    TypeConceptKey = Guid.Parse("14d69b32-f6c4-4a49-a527-a74893dbcf4a"), // Order
                    ActTime = adv.despatchInformation.despatchDateTimeSpecified ? adv.despatchInformation.despatchDateTime : DateTime.Now,
                    Extensions = new List<ActExtension>()
                    {
                        new ActExtension(Gs1ModelExtensions.ActualShipmentDate, typeof(DateExtensionHandler), adv.despatchInformation.actualShipDateTime),
                        new ActExtension(Gs1ModelExtensions.ExpectedDeliveryDate, typeof(DateExtensionHandler), adv.despatchInformation.estimatedDeliveryDateTime)
                    },
                    Tags = new List<ActTag>()
                    {
                        new ActTag("orderNumber", adv.despatchAdviceIdentification.entityIdentification),
                        new ActTag("orderStatus", "shipped"),
                        new ActTag("http://santedb.org/tags/contrib/importedData", "true")
                    },
                    Identifiers = new List<ActIdentifier>()
                    {
                        new ActIdentifier(issuingAuthority, adv.despatchAdviceIdentification.entityIdentification)
                    },
                    Participations = new List<ActParticipation>()
                    {
                        // TODO: Author
                        // TODO: Performer
                        new ActParticipation(ActParticipationKey.Location, sourceLocation.Key),
                        new ActParticipation(ActParticipationKey.Destination, destinationLocation.Key)
                    }
                };
                orderTransaction.Add(fulfillAct);

                // Fullfillment
                if (orderRequestAct != null)
                    fulfillAct.Relationships = new List<ActRelationship>()
                    {
                        new ActRelationship(ActRelationshipTypeKeys.Fulfills, orderRequestAct.Key)
                    };


                // Now add participations for each material in the despatch
                foreach (var dal in adv.despatchAdviceLogisticUnit)
                {
                    foreach (var line in dal.despatchAdviceLineItem)
                    {
                        if (line.despatchedQuantity.measurementUnitCode != "dose" &&
                            line.despatchedQuantity.measurementUnitCode != "unit")
                            throw new InvalidOperationException("Despatched quantity must be reported in units or doses");

                        var material = this.m_gs1Util.GetManufacturedMaterial(line.transactionalTradeItem, this.m_configuration.AutoCreateMaterials);

                        // Add a participation
                        fulfillAct.Participations.Add(new ActParticipation(ActParticipationKey.Consumable, material.Key)
                        {
                            Quantity = (int)line.despatchedQuantity.Value
                        });
                    }
                }

            }

            // insert transaction
            if (orderTransaction.Item.Count > 0)
                try
                {
                    ApplicationServiceContext.Current.GetService<IRepositoryService<Bundle>>().Insert(orderTransaction);
                }
                catch (Exception e)
                {
                    this.m_tracer.TraceError("Error issuing despatch advice: {0}", e);
                    throw new Exception($"Error issuing despatch advice: {e.Message}", e);
                }
        }

        /// <summary>
        /// Requests the issuance of a BMS1 inventory report request
        /// </summary>
        public LogisticsInventoryReportMessageType IssueInventoryReportRequest(LogisticsInventoryReportRequestMessageType parameters)
        {
            // Status
            LogisticsInventoryReportMessageType retVal = new LogisticsInventoryReportMessageType()
            {
                StandardBusinessDocumentHeader = this.m_gs1Util.CreateDocumentHeader("logisticsInventoryReport", null)
            };


            // Date / time of report

            DateTime? reportFrom = parameters.logisticsInventoryReportRequest.First().reportingPeriod?.beginDate ?? DateTime.MinValue,
                reportTo = parameters.logisticsInventoryReportRequest.First().reportingPeriod?.endDate ?? DateTime.Now;

            // return value
            LogisticsInventoryReportType report = new LogisticsInventoryReportType()
            {
                creationDateTime = DateTime.Now,
                documentStatusCode = DocumentStatusEnumerationType.ORIGINAL,
                documentActionCode = DocumentActionEnumerationType.CHANGE_BY_REFRESH,
                logisticsInventoryReportIdentification = new Ecom_EntityIdentificationType() { entityIdentification = BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0).ToString("X") },
                structureTypeCode = new StructureTypeCodeType() { Value = "LOCATION_BY_ITEM" },
                documentActionCodeSpecified = true
            };

            var locationStockStatuses = new List<LogisticsInventoryReportInventoryLocationType>();

            // Next, we want to know which facilities for which we're getting the inventory report
            List<Place> filterPlaces = null;
            if (parameters.logisticsInventoryReportRequest.First().logisticsInventoryRequestLocation != null &&
                parameters.logisticsInventoryReportRequest.First().logisticsInventoryRequestLocation.Length > 0)
            {
                foreach (var filter in parameters.logisticsInventoryReportRequest.First().logisticsInventoryRequestLocation)
                {
                    int tc = 0;
                    var id = filter.inventoryLocation.gln ?? filter.inventoryLocation.additionalPartyIdentification?.FirstOrDefault()?.Value;
                    var place = this.m_placeRepository.Find(o => o.Identifiers.Any(i => i.Value == id), 0, 1, out tc).FirstOrDefault();
                    if (place == null)
                    {
                        Guid uuid = Guid.Empty;
                        if (Guid.TryParse(id, out uuid))
                            place = this.m_placeRepository.Get(uuid, Guid.Empty);

                        if(place == null)
                            throw new FileNotFoundException($"Place {filter.inventoryLocation.gln} not found");
                    }
                    if (filterPlaces == null)
                        filterPlaces = new List<Place>() { place };
                    else
                        filterPlaces.Add(place);
                }
            }
            else
                filterPlaces = this.m_placeRepository.Find(o => o.ClassConceptKey == EntityClassKeys.ServiceDeliveryLocation).ToList();

            // Get the GLN AA data
            var oidService = ApplicationServiceContext.Current.GetService<IAssigningAuthorityRepositoryService>();
            var gln = oidService.Get("GLN");
            var gtin = oidService.Get("GTIN");

            if (gln == null || gln.Oid == null)
                throw new InvalidOperationException("GLN configuration must carry OID and be named GLN in repository");
            if (gtin == null || gtin.Oid == null)
                throw new InvalidOperationException("GTIN configuration must carry OID and be named GTIN in repository");

            var masterAuthContext = AuthenticationContext.Current;

            // Create the inventory report
            filterPlaces.AsParallel().ForAll(place =>
            {
                try
                {
                    AuthenticationContext.Current = masterAuthContext;

                    var locationStockStatus = new LogisticsInventoryReportInventoryLocationType();
                    lock (locationStockStatuses)
                        locationStockStatuses.Add(locationStockStatus);

                    // TODO: Store the GLN configuration domain name
                    locationStockStatus.inventoryLocation = this.m_gs1Util.CreateLocation(place);

                    var tradeItemStatuses = new List<TradeItemInventoryStatusType>();

                    // What are the relationships of held entities
                    var persistenceService = ApplicationServiceContext.Current.GetService<IDataPersistenceService<EntityRelationship>>();
                    var relationships = persistenceService.Query(o=>o.RelationshipTypeKey == EntityRelationshipTypeKeys.OwnedEntity && o.SourceEntityKey == place.Key.Value, AuthenticationContext.Current.Principal);
                    relationships.AsParallel().ForAll(rel =>
                    {
                        AuthenticationContext.Current = masterAuthContext;

                        if (!(rel.TargetEntity is ManufacturedMaterial))
                        {
                            var matl = this.m_manufMaterialRepository.Get(rel.TargetEntityKey.Value, Guid.Empty);
                            if (matl == null)
                            {
                                Trace.TraceWarning("It looks like {0} owns {1} but {1} is not a mmat!?!?!", place.Key, rel.TargetEntityKey);
                                return;
                            }
                            else
                                rel.TargetEntity = matl;
                        }
                        var mmat = rel.TargetEntity as ManufacturedMaterial;
                        if (!(mmat is ManufacturedMaterial))
                            return;

                        var mat = this.m_materialRepository.Find(o => o.Relationships.Where(r => r.RelationshipType.Mnemonic == "Instance").Any(r => r.TargetEntity.Key == mmat.Key)).FirstOrDefault();
                        var instanceData = mat.LoadCollection<EntityRelationship>("Relationships").FirstOrDefault(o => o.RelationshipTypeKey == EntityRelationshipTypeKeys.Instance);

                        decimal balanceOH = rel.Quantity ?? 0;

                        // get the adjustments the adjustment acts are allocations and transfers
                        var adjustments = this.m_stockService.FindAdjustments(mmat.Key.Value, place.Key.Value, reportFrom, reportTo);

                        // We want to roll back to the start time and re-calc balance oh at time?
                        if (reportTo.Value.Date < DateTime.Now.Date)
                        {
                            var consumed = this.m_stockService.GetConsumed(mmat.Key.Value, place.Key.Value, reportTo, DateTime.Now);
                            balanceOH -= (decimal)consumed.Sum(o => o.Quantity ?? 0);

                            if (balanceOH == 0 && this.m_stockService.GetConsumed(mmat.Key.Value, place.Key.Value, reportFrom, reportTo).Count() == 0)
                                return;
                        }

                        ReferenceTerm cvx = null;
                        if (mat.TypeConceptKey.HasValue)
                            cvx = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>().GetConceptReferenceTerm(mat.TypeConceptKey.Value, "CVX");

                        var typeItemCode = new ItemTypeCodeType()
                        {
                            Value = cvx?.Mnemonic ?? mmat.TypeConcept?.Mnemonic ?? mat.Key.Value.ToString(),
                            codeListVersion = cvx?.LoadProperty<CodeSystem>("CodeSystem")?.Authority ?? "SanteDB-MaterialType"
                        };

                        // First we need the GTIN for on-hand balance
                        lock(tradeItemStatuses)
                            tradeItemStatuses.Add(new TradeItemInventoryStatusType()
                            {
                                gtin = mmat.Identifiers.FirstOrDefault(o => o.Authority.DomainName == "GTIN")?.Value,
                                itemTypeCode = typeItemCode,
                                additionalTradeItemIdentification = mmat.Identifiers.Where(o => o.Authority.DomainName != "GTIN").Select(o => new AdditionalTradeItemIdentificationType()
                                {
                                    additionalTradeItemIdentificationTypeCode = o.Authority.DomainName,
                                    Value = o.Value
                                }).ToArray(),
                                tradeItemDescription = mmat.Names.Select(o => new Description200Type() { Value = o.Component.FirstOrDefault()?.Value }).FirstOrDefault(),
                                tradeItemClassification = new TradeItemClassificationType()
                                {
                                    additionalTradeItemClassificationCode = mat.Identifiers.Where(o => o.Authority.Oid != gtin.Oid).Select(o => new AdditionalTradeItemClassificationCodeType()
                                    {
                                        codeListVersion = o.Authority.DomainName,
                                        Value = o.Value
                                    }).ToArray()
                                },
                                inventoryDateTime = DateTime.Now,
                                inventoryDispositionCode = new InventoryDispositionCodeType() { Value = "ON_HAND" },
                                transactionalItemData = new TransactionalItemDataType[]
                                {
                                new TransactionalItemDataType()
                                {
                                    tradeItemQuantity = new QuantityType()
                                    {
                                        measurementUnitCode = (mmat.QuantityConcept ?? mat?.QuantityConcept)?.ReferenceTerms.Select(o => new AdditionalLogisticUnitIdentificationType()
                                        {
                                            additionalLogisticUnitIdentificationTypeCode = o.ReferenceTerm.CodeSystem.Name,
                                            Value = o.ReferenceTerm.Mnemonic
                                        }).FirstOrDefault()?.Value,
                                        Value = balanceOH
                                    },
                                    batchNumber = mmat.LotNumber,
                                    itemExpirationDate = mmat.ExpiryDate.Value,
                                    itemExpirationDateSpecified = true
                                }
                                }
                            });


                        foreach (var adjgrp in adjustments.GroupBy(o => o.ReasonConceptKey))
                        {
                            var reasonConcept = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>().GetConceptReferenceTerm(adjgrp.Key.Value, "GS1_STOCK_STATUS")?.Mnemonic;
                            if (reasonConcept == null)
                                reasonConcept = (ApplicationServiceContext.Current.GetService<IConceptRepositoryService>().Get(adjgrp.Key.Value, Guid.Empty) as Concept)?.Mnemonic;

                            // Broken vials?
                            lock(tradeItemStatuses)
                                tradeItemStatuses.Add(new TradeItemInventoryStatusType()
                                {
                                    gtin = mmat.Identifiers.FirstOrDefault(o => o.Authority.DomainName == "GTIN")?.Value,
                                    itemTypeCode = typeItemCode,
                                    additionalTradeItemIdentification = mmat.Identifiers.Where(o => o.Authority.DomainName != "GTIN").Select(o => new AdditionalTradeItemIdentificationType()
                                    {
                                        additionalTradeItemIdentificationTypeCode = o.Authority.DomainName,
                                        Value = o.Value
                                    }).ToArray(),
                                    tradeItemClassification = new TradeItemClassificationType()
                                    {
                                        additionalTradeItemClassificationCode = mat.Identifiers.Where(o => o.Authority.Oid != gtin.Oid).Select(o => new AdditionalTradeItemClassificationCodeType()
                                        {
                                            codeListVersion = o.Authority.DomainName,
                                            Value = o.Value
                                        }).ToArray()
                                    },
                                    tradeItemDescription = mmat.Names.Select(o => new Description200Type() { Value = o.Component.FirstOrDefault()?.Value }).FirstOrDefault(),
                                    inventoryDateTime = DateTime.Now,
                                    inventoryDispositionCode = new InventoryDispositionCodeType() { Value = reasonConcept },
                                    transactionalItemData = new TransactionalItemDataType[]
                                    {
                                        new TransactionalItemDataType()
                                        {
                                            transactionalItemLogisticUnitInformation = instanceData == null ? null : new TransactionalItemLogisticUnitInformationType()
                                            {
                                              numberOfLayers = "1",
                                              numberOfUnitsPerLayer = instanceData.Quantity.ToString(),
                                              packageTypeCode = new PackageTypeCodeType() { Value = mat.LoadCollection<EntityExtension>("Extensions").FirstOrDefault(o=>o.ExtensionTypeKey == Gs1ModelExtensions.PackagingUnit)?.ExtensionValue?.ToString() ?? "CONT" }
                                            },
                                            tradeItemQuantity = new QuantityType()
                                            {
                                                measurementUnitCode = (mmat.QuantityConcept ?? mat?.QuantityConcept)?.ReferenceTerms.Select(o => new AdditionalLogisticUnitIdentificationType()
                                                {
                                                    additionalLogisticUnitIdentificationTypeCode = o.ReferenceTerm.CodeSystem.Name,
                                                    Value = o.ReferenceTerm.Mnemonic
                                                }).FirstOrDefault()?.Value,
                                                Value = Math.Abs(adjgrp.Sum(o => o.Participations.First(p => p.ParticipationRoleKey == ActParticipationKey.Consumable && p.PlayerEntityKey == mmat.Key).Quantity.Value))
                                            },
                                            batchNumber = mmat.LotNumber,
                                            itemExpirationDate = mmat.ExpiryDate.Value,
                                            itemExpirationDateSpecified = true
                                        }
                                    }
                                });
                        }

                    });

                    // Reduce
                    locationStockStatus.tradeItemInventoryStatus = tradeItemStatuses.ToArray();
                }
                catch (Exception e)
                {
                    traceSource.TraceError("Error fetching stock data : {0}", e);
                }
                // TODO: Reduce and Group by GTIN
            });

            report.logisticsInventoryReportInventoryLocation = locationStockStatuses.ToArray();
            retVal.logisticsInventoryReport = new LogisticsInventoryReportType[] { report };
            return retVal;
        }

        /// <summary>
        /// Issues the order response message which will mark the requested order as underway
        /// </summary>
        public void IssueOrderResponse(OrderResponseMessageType orderResponse)
        {
            // TODO: Validate the standard header

            Bundle orderTransaction = new Bundle();

            // Loop 
            foreach (var resp in orderResponse.orderResponse)
            {

                // Find the original order which this despatch advice is fulfilling
                Act orderRequestAct = this.m_gs1Util.GetOrder(resp.originalOrder, ActMoodKeys.Request);
                if (orderRequestAct == null)
                    throw new KeyNotFoundException("Could not find originalOrder");

                // Update the supplier if it exists
                Place sourceLocation = this.m_gs1Util.GetLocation(resp.seller);
                if (sourceLocation != null && !orderRequestAct.Participations.Any(o => o.ParticipationRoleKey == ActParticipationKey.Distributor))
                {
                    // Add participation
                    orderRequestAct.Participations.Add(new ActParticipation()
                    {
                        ActKey = orderRequestAct.Key,
                        PlayerEntityKey = sourceLocation.Key,
                        ParticipationRoleKey = ActParticipationKey.Distributor
                    });
                }
                else if (resp.seller != null && sourceLocation == null)
                {
                    throw new KeyNotFoundException($"Could not find seller id with {resp.seller?.additionalPartyIdentification?.FirstOrDefault()?.Value ?? resp.seller.gln}");
                }

                var oidService = ApplicationServiceContext.Current.GetService<IAssigningAuthorityRepositoryService>();
                var gln = oidService.Get("GLN");
                var issuingAuthority = oidService.Find(o=>o.Oid == $"{gln.Oid}.{resp.orderResponseIdentification.contentOwner.gln}").FirstOrDefault();
                if (issuingAuthority == null)
                    issuingAuthority = oidService.Get(this.m_configuration.DefaultContentOwnerAssigningAuthority);

                if (issuingAuthority == null)
                    throw new KeyNotFoundException("Cannot find default issuing authority for advice identification. Please configure a valid OID");

                orderRequestAct.Identifiers.Add(new ActIdentifier(issuingAuthority, resp.orderResponseIdentification.entityIdentification));

                // If the original order request is not comlete, then complete it
                var existingTag = orderRequestAct.Tags.FirstOrDefault(o => o.TagKey == "orderStatus");
                if (existingTag == null)
                {
                    existingTag = new ActTag("orderStatus", "");
                    orderRequestAct.Tags.Add(existingTag);
                }

                // Accepted or not
                if (resp.responseStatusCode?.Value == "ACCEPTED")
                    existingTag.Value = "accepted";
                else if (resp.responseStatusCode?.Value == "REJECTED")
                    existingTag.Value = "rejected";

                orderTransaction.Add(orderRequestAct);
            }

            // insert transaction
            try
            {
                ApplicationServiceContext.Current.GetService<IRepositoryService<Bundle>>().Insert(orderTransaction);
            }
            catch (Exception e)
            {
                this.m_tracer.TraceError("Error issuing despatch advice: {0}", e);
                throw new Exception($"Error issuing despatch advice: {e.Message}", e);
            }

        }
    }
}