using System;

using System.Reflection;
using System.Linq.Expressions;

namespace RxApp
{
    internal static class Reflection
    {
        /// http://blog.abodit.com/2011/09/convert-a-property-getter-to-a-setter/
        /// Convert a lambda expression for a getter into a setter
        internal static Action<T, U> GetSetter<T,U>(this Expression<Func<T, U>> This)
        {
            var memberExpression = (MemberExpression)This.Body;
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

