/*
 * Copyright 2015-2019 Mohawk College of Applied Arts and Technology
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
 * User: JustinFyfe
 * Date: 2019-1-22
 */
using SanteDB.Core.Services;
using System;
using System.Collections.Generic;

namespace SanteDB.Persistence.MDM.Services
{
    /// <summary>
    /// Represents a master match 
    /// </summary>
    public class MasterMatch
    {
        /// <summary>
        /// Gets the master UUID
        /// </summary>
        public Guid Master { get; set; }

        /// <summary>
        /// Gets the match result
        /// </summary>
        public IRecordMatchResult MatchResult { get; set; }

        /// <summary>
        /// Creates a new master match
        /// </summary>
        public MasterMatch(Guid master, IRecordMatchResult match)
        {
            this.MatchResult = match;
            this.Master = master;
        }
    }

    /// <summary>
    /// Equality comparer
    /// </summary>
    internal class MasterMatchEqualityComparer : IEqualityComparer<MasterMatch>
    {
        /// <summary>
        /// Determine if x equals y
        /// </summary>
        public bool Equals(MasterMatch x, MasterMatch y)
        {
            return x.Master == y.Master;
        }

        /// <summary>
        /// Get hash code
        /// </summary>
        public int GetHashCode(MasterMatch obj)
        {
            return obj.Master.GetHashCode();
        }
    }
}