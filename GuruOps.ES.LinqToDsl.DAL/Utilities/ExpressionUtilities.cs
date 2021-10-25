using System;
using System.Linq.Expressions;

namespace GuruOps.ES.LinqToDsl.DAL.Utilities
{
    public static class ExpressionUtilities
    {
        public static Expression<Func<T, bool>> AndAlso<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
        {
            Expression<Func<T, bool>> result;
            if (left == null)
            {
                result = right;
            }
            else if (right == null)
            {
                result = left;
            }
            else
            {
                var binary = Expression.AndAlso(left.Body, right.Body);
                result = Expression.Lambda<Func<T, bool>>(binary, Expression.Parameter(typeof(T)));
            }

            return result;
        }

        public static Expression<Func<T, bool>> OrElse<T>(Expression<Func<T, bool>> left, Expression<Func<T, bool>> right)
        {
            Expression<Func<T, bool>> result;
            if (left == null)
            {
                result = right;
            }
            else if (right == null)
            {
                result = left;
            }
            else
            {
                var binary = Expression.OrElse(left.Body, right.Body);
                result = Expression.Lambda<Func<T, bool>>(binary, Expression.Parameter(typeof(T)));
            }

            return result;
        }
    }
}
