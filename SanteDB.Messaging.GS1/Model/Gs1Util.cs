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
using SanteDB.Core;
using SanteDB.Core.Model;
using SanteDB.Core.Model.Acts;
using SanteDB.Core.Model.Collection;
using SanteDB.Core.Model.Constants;
using SanteDB.Core.Model.DataTypes;
using SanteDB.Core.Model.Entities;
using SanteDB.Core.Services;
using SanteDB.Messaging.GS1.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SanteDB.Messaging.GS1.Model
{
    /// <summary>
    /// GS1 Utility class
    /// </summary>
    public class Gs1Util
    {
        // Configuration
        private Gs1ConfigurationSection m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection< Gs1ConfigurationSection>();

        // Act repository
        private IRepositoryService<Act> m_actRepository;
        // Material repository
        private IRepositoryService<Material> m_materialRepository;
        private IRepositoryService<ManufacturedMaterial> m_manufMaterialRepository;
        // Place repository
        private IRepositoryService<Place>  m_placeRepository;
        // Stock service
        private IStockManagementRepositoryService m_stockService;

        /// <summary>
        /// GS1 Utility class
        /// </summary>
        public Gs1Util()
        {
            this.m_actRepository = ApplicationServiceContext.Current.GetService<IRepositoryService<Act>>();
            this.m_materialRepository = ApplicationServiceContext.Current.GetService<IRepositoryService<Material>>();
            this.m_manufMaterialRepository = ApplicationServiceContext.Current.GetService<IRepositoryService<ManufacturedMaterial>>();
            this.m_placeRepository = ApplicationServiceContext.Current.GetService<IRepositoryService<Place>>();
            this.m_stockService = ApplicationServiceContext.Current.GetService<IStockManagementRepositoryService>();
        }

        /// <summary>
        /// Converts the specified IMS place into a TransactionalPartyType
        /// </summary>
        public TransactionalPartyType CreateLocation(Place place)
        {
            if (place == null) return null;

            var oidService = ApplicationServiceContext.Current.GetService<IAssigningAuthorityRepositoryService>();
            var gln = oidService.Get("GLN");
            return new TransactionalPartyType()
            {
                gln = place.Identifiers.FirstOrDefault(o => o.Authority.Oid == gln.Oid)?.Value,
                address = new AddressType()
                {
                    state = place.Addresses.FirstOrDefault()?.Component.FirstOrDefault(o => o.ComponentTypeKey == AddressComponentKeys.State)?.Value,
                    city = place.Addresses.FirstOrDefault()?.Component.FirstOrDefault(o => o.ComponentTypeKey == AddressComponentKeys.City)?.Value,
                    countryCode = new CountryCodeType() { Value = place.Addresses.FirstOrDefault()?.Component.FirstOrDefault(o => o.ComponentTypeKey == AddressComponentKeys.Country)?.Value },
                    countyCode = place.Addresses.FirstOrDefault()?.Component.FirstOrDefault(o => o.ComponentTypeKey == AddressComponentKeys.County)?.Value,
                    postalCode = place.Addresses.FirstOrDefault()?.Component.FirstOrDefault(o => o.ComponentTypeKey == AddressComponentKeys.PostalCode)?.Value,
                },
                additionalPartyIdentification = place.Identifiers.Select(o => new AdditionalPartyIdentificationType()
                {
                    additionalPartyIdentificationTypeCode = o.Authority.DomainName,
                    Value = o.Value
                }).ToArray(),
                organisationDetails = new OrganisationType()
                {
                    organisationName = place.Names.FirstOrDefault()?.Component.FirstOrDefault()?.Value
                }
            };
        }

        /// <summary>
        /// Gets the specified facility from the gs1 party information
        /// </summary>
        public Place GetLocation(TransactionalPartyType gs1Party)
        {

            if (gs1Party == null) return null;

            Place retVal = null;
            int tr = 0;

            // First, we will attempt to look up by GLN
            if (!String.IsNullOrEmpty(gs1Party.gln))
            {
                retVal = this.m_placeRepository.Find(p => p.Identifiers.Any(o => o.Value == gs1Party.gln && o.Authority.DomainName == "GLN"), 0, 1, out tr).FirstOrDefault();
                if (retVal == null)
                    throw new KeyNotFoundException($"Facility with GLN {gs1Party.gln} not found");
            }

            // let's look it up by alternate identifier then
            foreach (var id in gs1Party.additionalPartyIdentification)
            {
                retVal = this.m_placeRepository.Find(p => p.Identifiers.Any(i => i.Value == id.Value && i.Authority.DomainName == id.additionalPartyIdentificationTypeCode), 0, 1, out tr).FirstOrDefault();
                if (retVal != null) break;
            }

            return retVal;
        }

        /// <summary>
        /// Get order information
        /// </summary>
        public Act GetOrder(Ecom_DocumentReferenceType documentReference, Guid moodConceptKey)
        {
            if (documentReference == null)
                throw new ArgumentNullException("documentReference", "Document reference must be supplied for correlation lookup");
            else if (String.IsNullOrEmpty(documentReference.entityIdentification))
                throw new ArgumentException("Document reference must carry entityIdentification", "documentReference");

            Guid orderId = Guid.Empty;
            Act retVal = null;

            if (Guid.TryParse(documentReference.entityIdentification, out orderId))
                retVal = this.m_actRepository.Get(orderId, Guid.Empty);
            if (retVal == null)
            {
                var oidService = ApplicationServiceContext.Current.GetService<IAssigningAuthorityRepositoryService>();
                var gln = oidService.Get("GLN");
                AssigningAuthority issuingAuthority = null;
                if (documentReference.contentOwner != null)
                    issuingAuthority = oidService.Find(o=>o.Oid == $"{gln.Oid}.{documentReference.contentOwner.gln}").FirstOrDefault();
                if (issuingAuthority == null)
                    issuingAuthority = oidService.Get(this.m_configuration.DefaultContentOwnerAssigningAuthority);
                if (issuingAuthority == null)
                    throw new InvalidOperationException("Could not find assigning authority linked with document reference owner. Please specify a default in the configuration");

                int tr = 0;
                retVal = this.m_actRepository.Find(o => o.ClassConceptKey == ActClassKeys.Supply && o.MoodConceptKey == moodConceptKey && o.Identifiers.Any(i => i.Value == documentReference.entityIdentification && i.AuthorityKey == issuingAuthority.Key), 0, 1, out tr).FirstOrDefault();
            }
            return retVal;
        }

        /// <summary>
        /// Create receive line item
        /// </summary>
        public ReceivingAdviceLogisticUnitType CreateReceiveLineItem(ActParticipation orderReceivePtcpt, ActParticipation orderSentPtcpt)
        {
            if (orderSentPtcpt == null)
                throw new ArgumentNullException(nameof(orderSentPtcpt), "Missing sending order participation");
            else if (orderReceivePtcpt == null)
                throw new ArgumentNullException(nameof(orderReceivePtcpt), "Missing receiving order participation");

            // Quantity code
            var quantityCode = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>().GetConceptReferenceTerm(orderReceivePtcpt.LoadProperty<Material>("PlayerEntity").QuantityConceptKey.Value, "UCUM");

            // Does the base material have it? 
            if (quantityCode == null)
            {
                var mat = this.m_materialRepository.Find(o => o.Relationships.Where(g => g.RelationshipType.Mnemonic == "Instance").Any(p => p.TargetEntityKey == orderReceivePtcpt.PlayerEntityKey)).FirstOrDefault();
                if (mat == null)
                    throw new InvalidOperationException($"Missing quantity code for {orderReceivePtcpt.LoadProperty<Material>("PlayerEntity").Key}");
                else
                {
                    quantityCode = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>().GetConceptReferenceTerm(mat.QuantityConceptKey.Value, "UCUM");
                    if (quantityCode == null)
                        throw new InvalidOperationException($"Missing quantity code for {orderReceivePtcpt.LoadProperty<Material>("PlayerEntity").Key}");

                }
            }
            // Receiving logistic unit type
            return new ReceivingAdviceLogisticUnitType()
            {
                receivingAdviceLineItem = new ReceivingAdviceLineItemType[]
                {
                    new ReceivingAdviceLineItemType()
                    {
                        quantityDespatched = new QuantityType()
                        {
                            Value = Math.Abs((decimal)orderSentPtcpt.Quantity),
                            measurementUnitCode = quantityCode.Mnemonic ?? "dose", codeListVersion = "UCUM"
                        },
                        quantityAccepted = new QuantityType()
                        {
                            Value = (decimal)orderReceivePtcpt.Quantity,
                            measurementUnitCode = quantityCode.Mnemonic ?? "dose", codeListVersion = "UCUM"
                        },
                        transactionalTradeItem = this.CreateTradeItem(orderReceivePtcpt.LoadProperty<Material>("PlayerEntity")),
                        receivingConditionInformation = new ReceivingConditionInformationType[]
                        {
                            new ReceivingConditionInformationType()
                            {
                                receivingConditionCode = new ReceivingConditionCodeType() { Value = "DAMAGED_PRODUCT_OR_CONTAINER" },
                                receivingConditionQuantity = new QuantityType()
                                {
                                    Value = (decimal)(Math.Abs(orderSentPtcpt.Quantity.Value) - orderReceivePtcpt.Quantity),
                                    measurementUnitCode = quantityCode.Mnemonic ?? "dose", codeListVersion = "UCUM"
                                }
                            },
                            new ReceivingConditionInformationType()
                            {
                                receivingConditionCode = new ReceivingConditionCodeType() { Value = "GOOD_CONDITION" },
                                receivingConditionQuantity = new QuantityType()
                                {
                                    Value = (decimal)orderReceivePtcpt.Quantity,
                                    measurementUnitCode = quantityCode.Mnemonic ?? "dose", codeListVersion = "UCUM"
                                }
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Order line item type
        /// </summary>
        public OrderLineItemType CreateOrderLineItem(ActParticipation participation)
        {
            var material = participation.LoadProperty<Material>("PlayerEntity");
            var quantityCode = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>().GetConceptReferenceTerm(material.QuantityConceptKey.Value, "UCUM");
            return new OrderLineItemType()
            {
                requestedQuantity = new QuantityType() { Value = (decimal)participation.Quantity, measurementUnitCode = quantityCode?.Mnemonic ?? "dose", codeListVersion = "UCUM" },
                additionalOrderLineInstruction =
                    material.LoadProperty<Concept>("TypeConcept")?.Mnemonic.StartsWith("VaccineType") == true ?
                        new Description200Type[] {
                            new Description200Type() { languageCode = "en", Value = "FRAGILE" },
                            new Description200Type() { languageCode = "en", Value = "KEEP REFRIGERATED" }
                        } : null,
                transactionalTradeItem = this.CreateTradeItem(material)
            };
        }

        /// <summary>
        /// Create a trade item
        /// </summary>
        public TransactionalTradeItemType CreateTradeItem(Material material)
        {
            if (material == null)
                throw new ArgumentNullException(nameof(material), "Missing material instance");

            ReferenceTerm cvx = null;
            if (material.TypeConceptKey.HasValue)
                cvx = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>().GetConceptReferenceTerm(material.TypeConceptKey.Value, "CVX");
            var typeItemCode = new ItemTypeCodeType()
            {
                Value = cvx?.Mnemonic ?? material.TypeConcept?.Mnemonic ?? material.Key.Value.ToString(),
                codeListVersion = cvx?.LoadProperty<CodeSystem>("CodeSystem")?.Authority ?? "SanteDB-MaterialType"
            };

            // Manufactured material?
            if (material is ManufacturedMaterial)
            {
                var mmat = material as ManufacturedMaterial;
                var mat = this.m_materialRepository.Find(o => o.Relationships.Where(r => r.RelationshipType.Mnemonic == "Instance").Any(r => r.TargetEntity.Key == mmat.Key)).FirstOrDefault();

                return new TransactionalTradeItemType()
                {
                    additionalTradeItemIdentification = material.LoadCollection<EntityIdentifier>("Identifiers").Where(o => o.Authority.DomainName != "GTIN").Select(o => new AdditionalTradeItemIdentificationType()
                    {
                        Value = o.Value,
                        additionalTradeItemIdentificationTypeCode = o.LoadProperty<AssigningAuthority>("Authority").DomainName
                    }).ToArray(),
                    tradeItemClassification = new TradeItemClassificationType()
                    {
                        additionalTradeItemClassificationCode = mat.LoadCollection<EntityIdentifier>("Identifiers").Select(o => new AdditionalTradeItemClassificationCodeType()
                        {
                            Value = o.Value,
                            codeListVersion = o.LoadProperty<AssigningAuthority>("Authority").DomainName
                        }).ToArray()
                    },
                    gtin = material.Identifiers.FirstOrDefault(o => o.Authority.DomainName == "GTIN").Value,
                    itemTypeCode = typeItemCode,
                    tradeItemDescription = material.Names.Select(o => new Description200Type() { Value = o.Component.FirstOrDefault()?.Value }).FirstOrDefault(),
                    transactionalItemData = new TransactionalItemDataType[]
                    {
                        new TransactionalItemDataType() {
                            batchNumber = mmat.LotNumber,
                            itemExpirationDate = mmat.ExpiryDate.Value,
                            itemExpirationDateSpecified = true
                        }
                    }
                };
            }
            else // Material code
            {
                return new TransactionalTradeItemType()
                {
                    tradeItemClassification = new TradeItemClassificationType()
                    {
                        additionalTradeItemClassificationCode = material.LoadCollection<EntityIdentifier>("Identifiers").Select(o => new AdditionalTradeItemClassificationCodeType()
                        {
                            Value = o.Value,
                            codeListVersion = o.LoadProperty<AssigningAuthority>("Authority").DomainName
                        }).ToArray()
                    },
                    itemTypeCode = typeItemCode,
                    tradeItemDescription = cvx?.LoadCollection<ReferenceTermName>("DisplayNames")?.Select(o => new Description200Type() { Value = o.Name })?.FirstOrDefault() ??
                        material.Names.Select(o => new Description200Type() { Value = o.Component.FirstOrDefault()?.Value }).FirstOrDefault(),
                };
            }
        }

        /// <summary>
        /// Gets the manufactured material from the specified trade item
        /// </summary>
        public ManufacturedMaterial GetManufacturedMaterial(TransactionalTradeItemType tradeItem, bool createIfNotFound = false)
        {
            if (tradeItem == null)
                throw new ArgumentNullException("tradeItem", "Trade item must have a value");
            else if (String.IsNullOrEmpty(tradeItem.gtin))
                throw new ArgumentException("Trade item is missing GTIN", "tradeItem");


            var oidService = ApplicationServiceContext.Current.GetService<IAssigningAuthorityRepositoryService>();
            var gtin = oidService.Get("GTIN");

            // Lookup material by lot number / gtin
            int tr = 0;
            var lotNumberString = tradeItem.transactionalItemData[0].lotNumber;
            ManufacturedMaterial retVal = this.m_manufMaterialRepository.Find(m => m.Identifiers.Any(o => o.Value == tradeItem.gtin && o.Authority.DomainName == "GTIN") && m.LotNumber == lotNumberString, 0, 1, out tr).FirstOrDefault();
            if (retVal == null && createIfNotFound)
            {
                var additionalData = tradeItem.transactionalItemData[0];
                if (!additionalData.itemExpirationDateSpecified)
                    throw new InvalidOperationException("Cannot auto-create material, expiration date is missing");

                // Material
                retVal = new ManufacturedMaterial()
                {
                    Key = Guid.NewGuid(),
                    LotNumber = additionalData.lotNumber,
                    Identifiers = new List<EntityIdentifier>()
                    {
                        new EntityIdentifier(gtin, tradeItem.gtin)
                    },
                    ExpiryDate = additionalData.itemExpirationDate,
                    Names = new List<EntityName>()
                    {
                        new EntityName(NameUseKeys.Assigned, tradeItem.tradeItemDescription.Value)
                    },
                    StatusConceptKey = StatusKeys.Active,
                    QuantityConceptKey = Guid.Parse("a4fc5c93-31c2-4f87-990e-c5a4e5ea2e76"),
                    Quantity = 1
                };

                // Store additional identifiers
                if (tradeItem.additionalTradeItemIdentification != null)
                    foreach (var id in tradeItem.additionalTradeItemIdentification)
                    {
                        var oid = oidService.Get(id.additionalTradeItemIdentificationTypeCode);
                        if (oid == null) continue;
                        retVal.Identifiers.Add(new EntityIdentifier(oid, id.Value));
                    }

                if (String.IsNullOrEmpty(tradeItem.itemTypeCode?.Value))
                    throw new InvalidOperationException("Cannot auto-create material, type code must be specified");
                else // lookup type code
                {
                    var concept = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>().FindConceptsByReferenceTerm(tradeItem.itemTypeCode.Value, tradeItem.itemTypeCode.codeListVersion).FirstOrDefault();
                    if (concept == null && tradeItem.itemTypeCode.codeListVersion == "SanteDB-MaterialType")
                        concept = ApplicationServiceContext.Current.GetService<IConceptRepositoryService>().GetConcept(tradeItem.itemTypeCode.Value);

                    // Type code not found
                    if (concept == null)
                        throw new InvalidOperationException($"Material type {tradeItem.itemTypeCode.Value} is not a valid concept");

                    // Get the material and set the type
                    retVal.TypeConceptKey = concept.Key;

                }

                // Find the type of material
                Material materialReference = null;
                if (tradeItem.tradeItemClassification != null)
                    foreach (var id in tradeItem.tradeItemClassification.additionalTradeItemClassificationCode)
                    {
                        materialReference = this.m_materialRepository.Find(o => o.Identifiers.Any(i => i.Value == id.Value && i.Authority.DomainName == id.codeListVersion) && o.ClassConceptKey == EntityClassKeys.Material && o.StatusConceptKey != StatusKeys.Obsolete, 0, 1, out tr).SingleOrDefault();
                        if (materialReference != null) break;
                    }
                if (materialReference == null)
                    materialReference = this.m_materialRepository.Find(o => o.TypeConceptKey == retVal.TypeConceptKey && o.ClassConceptKey == EntityClassKeys.Material && o.StatusConceptKey != StatusKeys.Obsolete, 0, 1, out tr).SingleOrDefault();
                if (materialReference == null)
                    throw new InvalidOperationException("Cannot find the base Material from trade item type code");

                // Material relationship
                EntityRelationship materialRelationship = new EntityRelationship()
                {
                    RelationshipTypeKey = EntityRelationshipTypeKeys.Instance,
                    Quantity = (int)(additionalData.tradeItemQuantity?.Value ?? 1),
                    SourceEntityKey = materialReference.Key,
                    TargetEntityKey = retVal.Key,
                    EffectiveVersionSequenceId = materialReference.VersionSequence
                };

                // TODO: Manufacturer won't be known

                // Insert the material && relationship
                ApplicationServiceContext.Current.GetService<IRepositoryService<Bundle>>().Insert(new Bundle()
                {
                    Item = new List<IdentifiedData>()
                    {
                        retVal,
                        materialRelationship
                    }
                });

            }
            else if (tradeItem.additionalTradeItemIdentification != null) // We may want to keep track of other identifiers this software knows as
            {
                bool shouldSave = false;
                foreach (var id in tradeItem.additionalTradeItemIdentification)
                {
                    var oid = oidService.Get(id.additionalTradeItemIdentificationTypeCode);
                    if (oid == null) continue;
                    if (!retVal.Identifiers.Any(o => o.AuthorityKey == oid.Key))
                    {
                        retVal.Identifiers.Add(new EntityIdentifier(oid, id.Value));
                        shouldSave = true;
                    }
                }

                if (shouldSave)
                    this.m_materialRepository.Save(retVal);

            }

            return retVal;
        }

        /// <summary>
        /// Creates the document header
        /// </summary>
        /// <returns></returns>
        public StandardBusinessDocumentHeader CreateDocumentHeader(String messageType, Entity senderInformation)
        {
            return new StandardBusinessDocumentHeader()
            {
                HeaderVersion = "1.0",
                DocumentIdentification = new DocumentIdentification()
                {
                    Standard = "GS1",
                    TypeVersion = "3.3",
                    InstanceIdentifier = BitConverter.ToInt64(Guid.NewGuid().ToByteArray(), 0).ToString("X"),
                    Type = messageType,
                    MultipleType = false,
                    MultipleTypeSpecified = true,
                    CreationDateAndTime = DateTime.Now
                },
                Sender = senderInformation == null ? new Partner[] {
                        new Partner() {
                            Identifier = new PartnerIdentification() {  Authority = this.m_configuration.PartnerIdentificationAuthority, Value = this.m_configuration.PartnerIdentification },
                            ContactInformation = new ContactInformation[] {
                                new ContactInformation()
                                {
                                    Contact = this.m_configuration.SenderContactEmail,
                                    ContactTypeIdentifier = "EMAIL"
                                }
                            }
                        }
                    } : new Partner[]
                    {
                        new Partner()
                        {
                            Identifier = new PartnerIdentification() { Authority = this.m_configuration.PartnerIdentificationAuthority, Value = senderInformation.Key.Value.ToString() },
                            ContactInformation = new ContactInformation[]
                            {
                                new ContactInformation()
                                {
                                    Contact = senderInformation.LoadCollection<EntityName>("Names").FirstOrDefault()?.ToString(),
                                    EmailAddress = senderInformation.LoadCollection<EntityTelecomAddress>("Telecoms").FirstOrDefault(o=>o.Value.Contains("@"))?.Value,
                                    TelephoneNumber = senderInformation.Telecoms.FirstOrDefault(o=>!o.Value.Contains("@"))?.Value
                                }
                            }
                    }
                }
            };
        }

    }
}
