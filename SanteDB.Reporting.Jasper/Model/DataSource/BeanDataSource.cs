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
using System.Xml.Serialization;

namespace SanteDB.Reporting.Jasper.Model.DataSource
{
    /// <summary>
    /// Represents a bean data source.
    /// </summary>
    /// <seealso cref="SanteDB.Reporting.Jasper.Model.ResourceBase" />
    [XmlType("beanDataSource")]
	[XmlRoot("beanDataSource")]
	public class BeanDataSource : ResourceBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BeanDataSource"/> class.
		/// </summary>
		public BeanDataSource()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BeanDataSource"/> class.
		/// </summary>
		/// <param name="beanName">Name of the bean.</param>
		/// <param name="beanMethod">The bean method.</param>
		public BeanDataSource(string beanName, string beanMethod)
		{
			this.BeanName = beanName;
			this.BeanMethod = beanMethod;
		}

		/// <summary>
		/// Gets or sets the bean method.
		/// </summary>
		/// <value>The bean method.</value>
		[XmlElement("beanMethod")]
		public string BeanMethod { get; set; }

		/// <summary>
		/// Gets or sets the name of the bean.
		/// </summary>
		/// <value>The name of the bean.</value>
		[XmlElement("beanName")]
		public string BeanName { get; set; }
	}
}