using Nest;
using System;
using System.Collections;
using System.Linq.Expressions;

namespace GuruOps.ES.LinqToDsl.DAL.ES.Linq.Nest
{
    public class AnyEqualityQueryTranslator
    {
        public QueryBase Create(Expression property, Expression left, Expression right)
        {
            QueryBase result = null;
            Extract(property, left, right, out Field field, out object value);


            if (value == null)
            {
                result = !new ExistsQuery { Field = field };
            }
            else
            {
                if (value is IEnumerable && !(value is string))
                {
                    foreach (var item in (IEnumerable)value)
                    {
                        result = result || new TermQuery { Field = field, Value = item };
                    }
                }
                else
                {
                    result = new TermQuery { Field = field, Value = value };
                }

            }
            return result;
        }

        public QueryBase Create(Expression property)
        {
            var field = FieldForAnyFactory.Create(property);

            QueryBase result = !new ExistsQuery { Field = field };

            return result;
        }

        private static void Extract(
            Expression property,
            Expression left,
            Expression right,
            out Field field,
            out object value)
        {

            left = ConvertQueryTranslator.ClearExpressionIfItConvert(left);
            right = ConvertQueryTranslator.ClearExpressionIfItConvert(right);

            if (left is MemberExpression && right is ConstantExpression)
            {
                field = FieldForAnyFactory.Create(property, (MemberExpression)left);
                value = ((ConstantExpression)right).Value;

            }
            else if (left is ConstantExpression && right is MemberExpression)
            {
                field = FieldForAnyFactory.Create(property, (MemberExpression)right);
                value = ((ConstantExpression)left).Value;
            }
            else
            {
                throw new NotSupportedException($"Right now we don't support Term query between expressions {left} and {right}.");
            }
        }
    }
}