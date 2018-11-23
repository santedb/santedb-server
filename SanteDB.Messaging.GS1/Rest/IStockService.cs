/*
 * Copyright 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: justin
 * Date: 2018-11-23
 */
using SanteDB.Messaging.GS1.Model;
using System.ServiceModel;
using RestSrvr;
using RestSrvr.Attributes;

namespace SanteDB.Messaging.GS1.Rest
{
	/// <summary>
	/// Stock service request
	/// </summary>
	[ServiceContract(Name = "GS1BMS")]
	public interface IStockService
	{
		/// <summary>
		/// Represents a request for issuance of an inventory report
		/// </summary>
		[RestInvoke(Method = "POST", UriTemplate = "/inventoryReport")]
		LogisticsInventoryReportMessageType IssueInventoryReportRequest(LogisticsInventoryReportRequestMessageType parameters);

        /// <summary>
        /// Represents a request to issue despatch advice
        /// </summary>
        [RestInvoke(Method = "POST", UriTemplate = "/despatchAdvice")]
        void IssueDespatchAdvice(DespatchAdviceMessageType advice);

        /// <summary>
        /// Issue receiving advice to the SanteDB IMS system
        /// </summary>
        /// TODO: Finish this
        //[RestInvoke(Method = "POST", RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Xml, UriTemplate = "/receivingAdvice")]
        //void IssueReceivingAdvice(ReceivingAdviceMessageType advice);

        /// <summary>
        /// Represents a request to issue an order
        /// </summary>
        /// TODO: Finish this
        //[RestInvoke(Method = "POST", RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Xml, UriTemplate = "/order")]
        //void IssueOrder(OrderMessageType order);

        /// <summary>
        /// Issues order response
        /// </summary>
        [RestInvoke(Method = "POST", UriTemplate = "/orderResponse")]
        void IssueOrderResponse(OrderResponseMessageType orderResponse);
    }
}