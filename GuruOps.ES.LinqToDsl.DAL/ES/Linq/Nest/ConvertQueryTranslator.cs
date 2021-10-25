using System.Linq.Expressions;

namespace GuruOps.ES.LinqToDsl.DAL.ES.Linq.Nest
{
    public static class ConvertQueryTranslator
    {
        public static Expression ClearExpressionIfItConvert(Expression expression)
        {
            return expression.NodeType == ExpressionType.Convert ? ((UnaryExpression)expression).Operand : expression;
        }
    }
}
