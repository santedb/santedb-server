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
using SanteDB.Reporting.Jasper.Model.Reference;
using System.Xml.Serialization;

namespace SanteDB.Reporting.Jasper.Model.Collection
{
    /// <summary>
    /// Represents a query.
    /// </summary>
    /// <seealso cref="SanteDB.Reporting.Jasper.Model.ResourceBase" />
    [XmlType("query")]
	[XmlRoot("query")]
	public class Query : ResourceBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Query"/> class.
		/// </summary>
		public Query()
		{
		}

		/// <summary>
		/// Gets or sets the data source reference.
		/// </summary>
		/// <value>The data source reference.</value>
		[XmlElement("dataSourceReference")]
		public DataSourceReference DataSourceReference { get; set; }

		/// <summary>
		/// Gets or sets the language.
		/// </summary>
		/// <value>The language.</value>
		[XmlElement("language")]
		public string Language { get; set; }

		/// <summary>
		/// Gets or sets the value.
		/// </summary>
		/// <value>The value.</value>
		[XmlElement("value")]
		public string Value { get; set; }
	}
}