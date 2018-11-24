﻿/*
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.Xml;

namespace SanteDB.Messaging.FHIR.Resources
{
    /// <summary>
    /// Raw XML Wrapper
    /// </summary>
    [Serializable]
    [XmlRoot("div", Namespace = "http://www.w3.org/1999/xhtml")]
    public class RawXmlWrapper
    {
        /// <summary>
        /// Represents any elements
        /// </summary>
        [XmlAnyElement]
        public XmlElement[] Elements { get; set; }

        /// <summary>
        /// Raw xml wrapper
        /// </summary>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public static implicit operator XmlElement[](RawXmlWrapper wrapper)
        {
            return wrapper.Elements;
        }

        /// <summary>
        /// Raw xml wrapper
        /// </summary>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        public static implicit operator RawXmlWrapper(XmlElement[] elements)
        {
            return new RawXmlWrapper() { Elements = elements };
        }
    }
}