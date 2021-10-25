using System;
using System.Linq.Expressions;
using GuruOps.ES.LinqToDsl.DAL.ES.Linq.Nest;
using Nest;

namespace GuruOps.ES.LinqToDsl.DAL.ES.Linq.Visitors
{
    public class SortingTranslator
    {
        public FieldSort Translate(Expression expression)
        {
            var result = new FieldSort();
            if (expression is MemberExpression memberExpression)
            {
                result.Field = FieldFactory.Create(memberExpression);
            }
            else if (expression is UnaryExpression unaryExpression)
            {
                if (unaryExpression.NodeType == ExpressionType.Convert)
                {
                    result.Field = FieldFactory.Create((MemberExpression)unaryExpression.Operand);
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported NodeType {unaryExpression.NodeType}, node.");
                }
            }
            else
            {
                throw new InvalidOperationException($"Not supported expression {expression}.");
            }
            return result;
        }
    }
}