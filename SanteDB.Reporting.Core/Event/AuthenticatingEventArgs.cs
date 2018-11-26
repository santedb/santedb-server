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
 * Date: 2018-6-22
 */
using System;

namespace SanteDB.Reporting.Core.Event
{
    /// <summary>
    /// Representing authenticating event arguments.
    /// </summary>
    /// <seealso cref="System.EventArgs" />
    public class AuthenticatingEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AuthenticatingEventArgs"/> class.
		/// </summary>
		public AuthenticatingEventArgs()
		{
		}
	}
}