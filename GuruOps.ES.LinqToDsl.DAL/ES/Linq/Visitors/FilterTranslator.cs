using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using GuruOps.ES.LinqToDsl.DAL.ES.Linq.Nest;
using GuruOps.ES.LinqToDsl.Models;
using Nest;

namespace GuruOps.ES.LinqToDsl.DAL.ES.Linq.Visitors
{
    public class FilterTranslator : GuardianVisitor
    {
        private QueryBase _query;
        private readonly EqualityQueryTranslator _equalityQueryTranslator;
        private readonly RangeQueryTranslator _rangeQueryTranslator;
        private readonly AnyEqualityQueryTranslator _anyEqualityQueryTranslator;

        public FilterTranslator(
            EqualityQueryTranslator equalityQueryTranslator,
            RangeQueryTranslator rangeQueryTranslator,
            AnyEqualityQueryTranslator anyEqualityQueryTranslator)
        {
            _equalityQueryTranslator = equalityQueryTranslator;
            _rangeQueryTranslator = rangeQueryTranslator;
            _anyEqualityQueryTranslator = anyEqualityQueryTranslator;
        }

        public QueryBase Translate(Expression node)
        {
            Visit(node);
            return _query;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            switch (node.Method.Name)
            {
                case "Equals":
                    switch (node.Arguments.Count)
                    {
                        case 1:
                            _query = _equalityQueryTranslator.Create(node.Object, node.Arguments[0]);
                            break;
                        case 2:
                            _query = _equalityQueryTranslator.Create(node.Arguments[0], node.Arguments[1]);
                            break;
                        default:
                            throw CreateNotSupportedException($"MethodCallExpression is not supported with {node.Arguments.Count} arguments.");
                    }
                    return node;
                case "Contains":
                    if (IsEnumerableExtension(node.Method.DeclaringType))
                    {
                        VisitContains(node.Arguments[0], node.Arguments[1]);
                        return node;
                    }
                    else if (IsICollection(node.Method.DeclaringType))
                    {
                        VisitContains(node.Object, node.Arguments[0]);
                        return node;
                    }
                    throw CreateNotSupportedException($"We support only Contains of {nameof(Enumerable)} and build-in implementation of ICollection.");
                
                case "Any":
                    if (node.Arguments.Count > 1)
                    {
                        VisitAny(node.Arguments[0], node.Arguments[1]);
                    }
                    else
                    {
                        VisitAny(node.Arguments[0]);
                    }
                    return node;
                
                default:
                    throw CreateNotSupportedException($"We don't support method {node.Method.Name} yet.");
            }
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Not:
                    {
                        _query = !CreateFilterTranslator()
                            .Translate(node.Operand);
                        return node;
                    }
                default:
                    throw CreateNotSupportedException($"{node.GetType().Name} - {node.NodeType} is not supported.");
            }
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            var isToLower = (node.Left is MethodCallExpression && ((MethodCallExpression)node.Left).Method.Name == "ToLower");
            switch (node.NodeType)
            {
                case ExpressionType.OrElse:
                    _query = CreateFilterTranslator().Translate(node.Left) ||
                            CreateFilterTranslator().Translate(node.Right);
                    break;
                case ExpressionType.AndAlso:
                    _query = CreateFilterTranslator().Translate(node.Left) &&
                            CreateFilterTranslator().Translate(node.Right);
                    break;
                case ExpressionType.Equal:
                    _query = node.Left.Type == typeof(RelatedDocument) ?
                        RelatedDocumentTermQuery.Create(node.Left, (ConstantExpression)node.Right) :
                        _equalityQueryTranslator.Create(node.Left, node.Right, isToLower);
                    break;
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                    _query = _rangeQueryTranslator.Create(node);
                    break;
                case ExpressionType.NotEqual:
                    _query = !_equalityQueryTranslator.Create(node.Left, node.Right, isToLower);
                    break;
                default:
                    throw CreateNotSupportedException($"{node.GetType().Name} - {node.NodeType} is not supported.");
            }
            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (IsInlineEquality(node))
            {
                _query = _equalityQueryTranslator.Create(node, Expression.Constant(true));
                return node;
            }
            throw CreateNotSupportedException($"Not supported member type - {node.Member.DeclaringType}.");
        }



        private void VisitAny(Expression property, Expression lambdaExpression)
        {
            var lambda = (LambdaExpression) lambdaExpression;
            var body = lambda.Body;           
            if (body is BinaryExpression && ((BinaryExpression) body).Method.Name == "op_Equality")
            {
                _query = _anyEqualityQueryTranslator.Create(property, ((BinaryExpression) body).Left, ((BinaryExpression) body).Right);
                return;
            }
            if (body is MethodCallExpression && ((MethodCallExpression)body).Method.Name == "Contains")
            {
               
                _query = _anyEqualityQueryTranslator.Create(property, ((MethodCallExpression)body).Arguments[0], ((MethodCallExpression)body).Object);
                return;                
            }

            throw CreateNotSupportedException($"Any method not support operation {((BinaryExpression)body).Method.Name}");
        }
        
        private void VisitAny(Expression property)
        {            
                _query = _anyEqualityQueryTranslator.Create(property);
                return;
        }

        private void VisitContains(Expression source, Expression match)
        {
            var isToLower = (match is MethodCallExpression && ((MethodCallExpression) match).Method.Name == "ToLower");
            if (source is ConstantExpression && (match is MemberExpression || isToLower))
            {
                var values = ((IEnumerable)((ConstantExpression)source).Value)
                    .Cast<object>()
                    .Distinct()
                    .ToList();
                foreach (var value in values)
                {
                    var matchQuery = _equalityQueryTranslator.Create(match, Expression.Constant(value), isToLower);
                    _query = _query != null ? _query | matchQuery : matchQuery;
                }
                return;
            }
            if (source is MemberExpression && match is ConstantExpression)
            {
                _query = match.Type == typeof(RelatedDocument) ?
                    RelatedDocumentTermQuery.Create(source, (ConstantExpression)match) :
                    _equalityQueryTranslator.Create(source, match);
                return;
            }

            throw CreateNotSupportedException(source is MemberExpression
                ? $"Match '{match}' in Contains operation must be a constant"
                : $"Unknown source '{source}' for Contains operation");
        }

        private FilterTranslator CreateFilterTranslator()
        {
            FilterTranslator result = new FilterTranslator(_equalityQueryTranslator, _rangeQueryTranslator, _anyEqualityQueryTranslator);
            return result;
        }

        private static bool IsInlineEquality(MemberExpression node)
        {
            bool result = node.Type == typeof(bool);
            return result;
        }

        private static bool IsICollection(Type type)
        {
            bool result = type.IsInstanceOf(typeof(System.Collections.Generic.ICollection<>));
            return result;
        }

        private static bool IsEnumerableExtension(Type type)
        {
            bool result = type == typeof(Enumerable);
            return result;
        }

        private static Exception CreateNotSupportedException(string message)
        {
            NotSupportedException result = new NotSupportedException(message);
            return result;
        }

    }
}
