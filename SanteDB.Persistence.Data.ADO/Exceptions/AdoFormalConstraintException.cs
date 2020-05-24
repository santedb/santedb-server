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
using SanteDB.Core;
using System;
using System.Data;

namespace SanteDB.Persistence.Data.ADO.Exceptions
{

    /// <summary>
    /// SQL Formal Constraint types
    /// </summary>
    public enum AdoFormalConstraintType
    {
        /// <summary>
        /// Indicates the identity was on an insert operation
        /// </summary>
        IdentityInsert,
        /// <summary>
        /// Indicates the identity was not present on an update or obsolete
        /// </summary>
        NonIdentityUpdate,
        /// <summary>
        /// Indicates a user attempted to update a readonly system object
        /// </summary>
        UpdatedReadonlyObject,
        /// <summary>
        /// Associated entity was persisted however there is no source object
        /// </summary>
        AssociatedEntityWithoutSourceKey,
        /// <summary>
        /// Attempated to insert an associated entity without an effective version
        /// </summary>
        AssociatedEntityWithoutEffectiveVersion
    }
    /// <summary>
    /// Represents a violation of a formal constraint
    /// </summary>
    [Serializable]
    public class AdoFormalConstraintException : ConstraintException
    {

        // The type of constraint violation
        private AdoFormalConstraintType m_violation;

        /// <summary>
        /// Formal constraint message
        /// </summary>
        public AdoFormalConstraintException(AdoFormalConstraintType violation) : base()
        {
            this.m_violation = violation;
        }

        /// <summary>
        /// Gets the violation type
        /// </summary>
        public AdoFormalConstraintType ViolationType
        {
            get { return this.m_violation; }
        }

        /// <summary>
        /// Gets the message of the exception
        /// </summary>
        public override string Message
        {
            get
            {
                return $"Formal Constraint Violation: {this.m_violation}";

            }
        }
    }
}
