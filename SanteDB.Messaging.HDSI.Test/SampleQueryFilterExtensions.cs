﻿/*
 * Copyright (C) 2021 - 2022, SanteSuite Inc. and the SanteSuite Contributors (See NOTICE.md for full copyright notices)
 * Copyright (C) 2019 - 2021, Fyfe Software Inc. and the SanteSuite Contributors
 * Portions Copyright (C) 2015-2018 Mohawk College of Applied Arts and Technology
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
 * User: fyfej
 * Date: 2022-5-30
 */
using SanteDB.Core.Model.Query;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace SanteDB.Messaging.HDSI.Test
{
    /// <summary>
    /// Simple extension methods
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class SimpleQueryExtensionMethods
    {
        /// <summary>
        /// Test expression 
        /// </summary>
        public static int TestExpression(this String me)
        {
            return 1;
        }

        /// <summary>
        /// Test expression 
        /// </summary>
        public static TimeSpan TestExpressionEx(this DateTime me, DateTime parm)
        {
            return parm.Subtract(me);
        }

        /// <summary>
        /// Boolean test
        /// </summary>
        public static bool BoolTest(this String me)
        {
            return true;
        }
    }

    /// <summary>
    /// Represents a simple query extension
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class SimpleQueryExtension : IQueryFilterExtension
    {
        /// <summary>
        /// Gets the name of the extension
        /// </summary>
        public string Name => "test";

        /// <summary>
        /// Gets the return type
        /// </summary>
        public MethodInfo ExtensionMethod => typeof(SimpleQueryExtensionMethods).GetRuntimeMethod("TestExpression", new Type[] { typeof(String) });

        /// <summary>
        /// Compose the expression
        /// </summary>
        public BinaryExpression Compose(Expression scope, ExpressionType comparison, Expression operand, Expression[] parms)
        {
            return Expression.MakeBinary(comparison,
                Expression.Call(
                        this.ExtensionMethod, scope)
                        , operand);
        }


    }

    /// <summary>
    /// A boolean query extension
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class BoolQueryExtension : IQueryFilterExtension
    {
        /// <summary>
        /// Get the name
        /// </summary>
        public string Name => "testBool";

        /// <summary>
        /// Get the extension method
        /// </summary>
        public MethodInfo ExtensionMethod => typeof(SimpleQueryExtensionMethods).GetRuntimeMethod("BoolTest", new Type[] { typeof(String) });

        /// <summary>
        /// Compose the expression
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="comparison"></param>
        /// <param name="valueExpression"></param>
        /// <param name="parms"></param>
        /// <returns></returns>
        public BinaryExpression Compose(Expression scope, ExpressionType comparison, Expression valueExpression, Expression[] parms)
        {
            return Expression.MakeBinary(ExpressionType.Equal,
                Expression.Call(
                        this.ExtensionMethod, scope)
                        , Expression.Constant(true));
        }
    }

    /// <summary>
    /// An extended query extension
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class SimpleQueryExtensionEx : IQueryFilterExtension
    {

        /// <summary>
        /// Gets the name
        /// </summary>
        public string Name => "testEx";

        /// <summary>
        /// Gets the return type
        /// </summary>
        public MethodInfo ExtensionMethod => typeof(SimpleQueryExtensionMethods).GetRuntimeMethod("TestExpressionEx", new Type[] { typeof(DateTime), typeof(DateTime) });

        /// <summary>
        /// Compose the expression
        /// </summary>
        public BinaryExpression Compose(Expression scope, ExpressionType comparison, Expression valueExpression, Expression[] parms)
        {
            Expression parmExpr = parms[0];
            if (parmExpr.Type.StripNullable() != typeof(DateTime) &&
                parmExpr is ConstantExpression)
            {
                parmExpr = Expression.Constant(DateTime.Parse((parmExpr as ConstantExpression).Value.ToString()));
            }

            return Expression.MakeBinary(comparison,
                Expression.Call(this.ExtensionMethod, new Expression[] {
                    scope,
                    parmExpr
                }), valueExpression);

        }
    }
}
