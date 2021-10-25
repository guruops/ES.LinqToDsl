using Nest;
using System;
using System.Linq.Expressions;

namespace GuruOps.ES.LinqToDsl.DAL.ES.Linq.Nest
{
    public static class RelatedDocumentsFieldFactory
    {
        private static readonly Type[] NoTypeArguments = null;

        public static Field Create(MemberExpression node, string propertyName = null)
        {
            Expression fieldExpression = null;
            var objectExpression = Expression.Convert(node, typeof(object));
            if (!string.IsNullOrEmpty(propertyName))
            {
                fieldExpression = Expression.Call(
                    typeof(SuffixExtensions),
                    "Suffix",
                    NoTypeArguments,
                    objectExpression,
                    Expression.Constant(propertyName));
                objectExpression = Expression.Convert(fieldExpression, typeof(object));
            }
            if (propertyName == "id")
            {
                fieldExpression = Expression.Call(
                    typeof(SuffixExtensions),
                    "Suffix",
                    NoTypeArguments,
                    objectExpression,
                    Expression.Constant("keyword"));
            }
            return new Field(fieldExpression);
        }
    }
}