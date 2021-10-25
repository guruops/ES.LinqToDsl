using System.Collections.Generic;
using System.Linq.Expressions;

namespace GuruOps.ES.LinqToDsl.DAL.ES.Linq.Visitors
{
    internal class EvaluatingExpressionVisitor : ExpressionVisitor
    {
        readonly HashSet<Expression> _chosenForEvaluation;

        private EvaluatingExpressionVisitor(HashSet<Expression> chosenForEvaluation)
        {
            _chosenForEvaluation = chosenForEvaluation;
        }

        public static Expression Evaluate(Expression e, HashSet<Expression> chosenForEvaluation)
        {
            EvaluatingExpressionVisitor evaluatingExpressionVisitor = new EvaluatingExpressionVisitor(chosenForEvaluation);
            Expression result = evaluatingExpressionVisitor.Visit(e);
            return result;
        }

        public override Expression Visit(Expression node)
        {
            if (node == null || node.NodeType == ExpressionType.Constant)
                return node;

            if (_chosenForEvaluation.Contains(node))
            {
                var compiled = Expression.Lambda(node)
                    .Compile()
                    .DynamicInvoke(null);
                return Expression.Constant(compiled, node.Type);
            }
            else
                return base.Visit(node);
        }
    }
}
