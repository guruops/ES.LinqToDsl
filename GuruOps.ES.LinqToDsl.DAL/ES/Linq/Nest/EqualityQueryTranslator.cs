using Nest;
using System;
using System.Linq.Expressions;

namespace GuruOps.ES.LinqToDsl.DAL.ES.Linq.Nest
{
    public class EqualityQueryTranslator
    {
        public QueryBase Create(Expression left, Expression right, bool isToLower = false)
        {
            QueryBase result;
            if (isToLower)
            {
                left = ((MethodCallExpression)left).Object;
            }
            Extract(left, right, out Field field, out object value, isToLower);
            if (value == null)
            {
                result = !new ExistsQuery { Field = field };
            }
            else
            {
                result = new TermQuery { Field = field, Value = value };
            }
            return result;
        }

        private static void Extract(
            Expression left,
            Expression right,
            out Field field,
            out object value,
            bool isToLower)
        {

            left = ConvertQueryTranslator.ClearExpressionIfItConvert(left);
            right = ConvertQueryTranslator.ClearExpressionIfItConvert(right);

            if (left is MemberExpression && right is ConstantExpression)
            {
                field = FieldFactory.Create((MemberExpression)left, isToLower);
                value = ((ConstantExpression)right).Value;

            }
            else if (left is ConstantExpression && right is MemberExpression)
            {
                field = FieldFactory.Create((MemberExpression)right, isToLower);
                value = ((ConstantExpression)left).Value;
            }
            else
            {
                throw new NotSupportedException($"Right now we don't support Term query between expressions {left} and {right}.");
            }
        }
    }
}
