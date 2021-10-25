using System;
using System.Linq.Expressions;
using System.Reflection;

namespace GuruOps.ES.LinqToDsl.DAL.ES.Linq.Visitors
{
    public class EnumerationExpressionVisitor : ExpressionVisitor
    {
        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.Equal || node.NodeType == ExpressionType.NotEqual)
            {
                if (TryExtractConvertExpression(node, out MemberExpression memberExpression, out object value))
                {
                    if (memberExpression.Member.MemberType == MemberTypes.Property)
                    {
                        var propertyType = (memberExpression.Member as PropertyInfo)
                            .PropertyType;
                        if (propertyType.IsEnum)
                        {
                            var enumValue = CastToEnum(propertyType, value);
                            var result = Expression.MakeBinary(
                                node.NodeType,
                                memberExpression,
                                Expression.Constant(enumValue));
                            return result;
                        }
                    }
                }
            }
            return base.VisitBinary(node);
        }

        private static bool TryExtractConvertExpression(
            BinaryExpression node,
            out MemberExpression memberExpression,
            out object value)
        {
            if (node.Left is UnaryExpression && node.Right is ConstantExpression)
            {
                var unaryExpression = (UnaryExpression)node.Left;
                var constantExpression = (ConstantExpression)node.Right;
                if (unaryExpression.NodeType == ExpressionType.Convert)
                {
                    memberExpression = (MemberExpression)unaryExpression.Operand;
                    value = constantExpression.Value;
                    return true;
                }
            }
            else if (node.Right is UnaryExpression && node.Left is ConstantExpression)
            {
                var unaryExpression = (UnaryExpression)node.Right;
                var constantExpression = (ConstantExpression)node.Left;
                if (unaryExpression.NodeType == ExpressionType.Convert)
                {
                    memberExpression = (MemberExpression)unaryExpression.Operand;
                    value = constantExpression.Value;
                    return true;
                }
            }

            memberExpression = null;
            value = null;

            return false;
        }

        private static object CastToEnum(Type type, object value)
        {
            var result = Enum.ToObject(type, value);
            return result;
        }
    }
}
