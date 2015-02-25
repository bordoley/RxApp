using System;

using System.Reflection;
using System.Linq.Expressions;

namespace RxApp
{
    internal static class Reflection
    {
        /// http://blog.abodit.com/2011/09/convert-a-property-getter-to-a-setter/
        /// Convert a lambda expression for a getter into a setter
        public static Action<T, U> GetSetter<T,U>(Expression<Func<T, U>> expression)
        {
            var memberExpression = (MemberExpression)expression.Body;
            var property = (PropertyInfo)memberExpression.Member;
            var setMethod = property.SetMethod;
         
            var parameterT = Expression.Parameter(typeof(T), "x");
            var parameterU = Expression.Parameter(typeof(U), "y");
         
            var newExpression =
                Expression.Lambda<Action<T, U>>(
                    Expression.Call(parameterT, setMethod, parameterU),
                    parameterT,
                    parameterU
                );
         
            return newExpression.Compile();
        }
    }
}

