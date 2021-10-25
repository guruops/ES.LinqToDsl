using Nest;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace GuruOps.ES.LinqToDsl.DAL.ES.Linq.Nest
{
    public class RangeQueryTranslator
    {
        private static readonly Dictionary<ExpressionType, OperatorType> Operators =
            new Dictionary<ExpressionType, OperatorType>()
            {
                {ExpressionType.GreaterThan, OperatorType.GreaterThan},
                {ExpressionType.GreaterThanOrEqual, OperatorType.GreaterThanOrEqual},
                {ExpressionType.LessThan, OperatorType.LessThan},
                {ExpressionType.LessThanOrEqual, OperatorType.LessThanOrEqual}
            };

        private static readonly Dictionary<OperatorType, OperatorType> OppositeOperators =
            new Dictionary<OperatorType, OperatorType>()
            {
                {OperatorType.GreaterThan,OperatorType.LessThan },
                {OperatorType.GreaterThanOrEqual, OperatorType.LessThanOrEqual},
                {OperatorType.LessThanOrEqual,OperatorType.GreaterThanOrEqual},
                {OperatorType.LessThan, OperatorType.GreaterThan}
            };

        public QueryBase Create(BinaryExpression node)
        {
            Extract(node, out Field field, out object value, out OperatorType operation);
            QueryBase result;
            if (value is DateTime dateTime)
            {
                result = CreateDateRange(field, operation, dateTime);
            }
            else
            {
                double doubleValue = Convert.ToDouble(value);
                result = CreateNumericRange(field, operation, doubleValue);
            }
            return result;
        }

        private static QueryBase CreateNumericRange(
            Field field,
            OperatorType operation,
            double value)
        {
            NumericRangeQuery result = new NumericRangeQuery { Field = field };
            switch (operation)
            {
                case OperatorType.GreaterThan:
                    result.GreaterThan = value;
                    break;
                case OperatorType.GreaterThanOrEqual:
                    result.GreaterThanOrEqualTo = value;
                    break;
                case OperatorType.LessThan:
                    result.LessThan = value;
                    break;
                case OperatorType.LessThanOrEqual:
                    result.LessThanOrEqualTo = value;
                    break;
                default:
                    throw new NotSupportedException($"Unexpected operation {operation}.");
            }

            return result;
        }

        private static QueryBase CreateDateRange(
            Field field,
            OperatorType operation,
            DateTime value)
        {
            if (value <= DateTime.MinValue)
            {
                value = DateTime.MinValue.AddMinutes(1);
            }
            DateRangeQuery result = new DateRangeQuery { Field = field };
            switch (operation)
            {
                case OperatorType.GreaterThan:
                    result.GreaterThan = value;
                    break;
                case OperatorType.GreaterThanOrEqual:
                    result.GreaterThanOrEqualTo = value;
                    break;
                case OperatorType.LessThan:
                    result.LessThan = value;
                    break;
                case OperatorType.LessThanOrEqual:
                    result.LessThanOrEqualTo = value;
                    break;
                default:
                    throw new NotSupportedException($"Unexpected operation {operation}.");
            }
            return result;
        }

        private static void Extract(
            BinaryExpression node,
            out Field field,
            out object value,
            out OperatorType operation)
        {
            var left = ConvertQueryTranslator.ClearExpressionIfItConvert(node.Left);
            var right = ConvertQueryTranslator.ClearExpressionIfItConvert(node.Right);

            if (left is MemberExpression && right is ConstantExpression)
            {
                field = CreateField(node.Left);
                value = ((ConstantExpression)right).Value;
                if (!Operators.TryGetValue(node.NodeType, out operation))
                {
                    throw new NotSupportedException($"Ups... Something went wrong. Unexpected operation - {node.NodeType}.");
                }
            }
            else if (left is ConstantExpression && right is MemberExpression)
            {
                field = CreateField(right);
                value = ((ConstantExpression)left).Value;
                if (Operators.TryGetValue(node.NodeType, out operation))
                {
                    operation = OppositeOperators[operation];
                }
                else
                {
                    throw new NotSupportedException($"Ups... Something went wrong. Unexpected operation - {node.NodeType}.");
                }
            }
            else
            {
                throw new NotSupportedException($"Right now we don't support Range comparison between expressions {left} and {right}.");
            }
        }

        private static Field CreateField(Expression node)
        {
            Field result = new Field(node);
            return result;
        }

        private enum OperatorType
        {
            LessThan,
            LessThanOrEqual,
            GreaterThan,
            GreaterThanOrEqual
        }
    }
}
