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
using SanteDB.Core.Http;
using SanteDB.Core.Interop.Clients;
using SanteDB.Core.Model.Serialization;
using SanteDB.Core.Services;
using SanteDB.Messaging.GS1.Configuration;
using SanteDB.Messaging.GS1.Model;
using System;
using System.IO;
using System.Xml.Serialization;

namespace SanteDB.Messaging.GS1.Transport.AS2
{
    /// <summary>
    /// GS1 service client
    /// </summary>
    public class Gs1ServiceClient : ServiceClientBase
    {

        // Configuration
        private As2ServiceElement m_configuration = ApplicationServiceContext.Current.GetService<IConfigurationManager>().GetSection<Gs1ConfigurationSection>()?.Gs1BrokerAddress;

        /// <summary>
        /// Create the GS1 service client
        /// </summary>
        public Gs1ServiceClient(IRestClient restClient) : base(restClient)
        {
            if (!string.IsNullOrEmpty(this.m_configuration.UserName))
                this.Client.Credentials = new HttpBasicCredentials(this.m_configuration.UserName, this.m_configuration.Password);

            this.Client.Accept = this.Client.Accept ?? "application/xml";
            this.m_configuration = this.Client.Description as As2ServiceElement;
        }

        /// <summary>
        /// Issue an order
        /// </summary>
        public void IssueOrder(OrderMessageType orderType)
        {
            String boundary = String.Format("------{0:N}", Guid.NewGuid());
            if (this.m_configuration.UseAS2MimeEncoding)
                this.Client.Post<MultipartAttachment, object>("orderRequest", String.Format("multipart/form-data; boundary={0}", boundary), this.CreateAttachment(orderType));
            else
                this.Client.Post<OrderMessageType, object>("orderRequest", "application/xml", orderType);
        }

        /// <summary>
        /// Issue an order
        /// </summary>
        public void IssueReceivingAdvice(ReceivingAdviceMessageType advice)
        {
            String boundary = String.Format("------{0:N}", Guid.NewGuid());
            if (this.m_configuration.UseAS2MimeEncoding)
                this.Client.Post<MultipartAttachment, object>("receivingAdvice", String.Format("multipart/form-data; boundary={0}", boundary), this.CreateAttachment(advice));
            else
                this.Client.Post<ReceivingAdviceMessageType, object>("receivingAdvice", "application/xml", advice);
        }

        /// <summary>
        /// Issue an order
        /// </summary>
        public void IssueDespatchAdvice(DespatchAdviceMessageType advice)
        {
            String boundary = String.Format("------{0:N}", Guid.NewGuid());
            if (this.m_configuration.UseAS2MimeEncoding)
                this.Client.Post<MultipartAttachment, object>("despatchAdvice", String.Format("multipart/form-data; boundary={0}", boundary), this.CreateAttachment(advice));
            else
                this.Client.Post<DespatchAdviceMessageType, object>("despatchAdvice", "application/xml", advice);
        }

        /// <summary>
        /// Create an appropriate MIME encoding
        /// </summary>
        private MultipartAttachment CreateAttachment(object content)
        {
            XmlSerializer xsz = XmlModelSerializerFactory.Current.CreateSerializer(content.GetType());
            using (var ms = new MemoryStream())
            {
                xsz.Serialize(ms, content);
                return new MultipartAttachment(ms.ToArray(), "edi/xml", "body", false);
            }
        }
    }
}
