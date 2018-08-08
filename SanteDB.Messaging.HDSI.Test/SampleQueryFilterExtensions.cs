using SanteDB.Core.Model.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SanteDB.Messaging.HDSI.Test
{
    /// <summary>
    /// Simple extension methods
    /// </summary>
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
    }

    /// <summary>
    /// Represents a simple query extension
    /// </summary>
    public class SimpleQueryExtension : IQueryFilterExtension
    {
        /// <summary>
        /// Gets the name of the extension
        /// </summary>
        public string Name => "test";

        /// <summary>
        /// Gets the return type
        /// </summary>
        public Type ReturnType => typeof(Int32);

        /// <summary>
        /// Compose the expression
        /// </summary>
        public BinaryExpression Compose(Expression scope, ExpressionType comparison, Expression operand, string[] parms)
        {
            return Expression.MakeBinary(comparison,
                Expression.Call(
                        typeof(SimpleQueryExtensionMethods).GetRuntimeMethod("TestExpression", new Type[] { typeof(String) }), scope)
                        , operand);
        }

        /// <summary>
        /// DeCompose the expression
        /// </summary>
        public KeyValuePair<string, object> DeCompose(BinaryExpression expression)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Detect whether the extension is present
        /// </summary>
        /// <param name="expression"></param>
        /// <returns></returns>
        public bool Detect(BinaryExpression expression)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// An extended query extension
    /// </summary>
    public class SimpleQueryExtensionEx : IQueryFilterExtension
    {

        /// <summary>
        /// Gets the name
        /// </summary>
        public string Name => "testEx";

        /// <summary>
        /// Gets the return type
        /// </summary>
        public Type ReturnType => typeof(TimeSpan);

        /// <summary>
        /// Compose the expression
        /// </summary>
        public BinaryExpression Compose(Expression scope, ExpressionType comparison, Expression valueExpression, string[] parms)
        {
            return Expression.MakeBinary(comparison,
                Expression.Call(typeof(SimpleQueryExtensionMethods).GetRuntimeMethod("TestExpressionEx", new Type[] { typeof(DateTime), typeof(DateTime) }), new Expression[] {
                    scope,
                    Expression.Constant(DateTime.Parse(parms[0]))
                }), valueExpression);

        }

        public KeyValuePair<string, object> DeCompose(BinaryExpression expression)
        {
            throw new NotImplementedException();
        }

        public bool Detect(BinaryExpression expression)
        {
            throw new NotImplementedException();
        }
    }
}
