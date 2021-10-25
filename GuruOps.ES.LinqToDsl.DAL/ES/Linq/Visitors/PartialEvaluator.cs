using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace GuruOps.ES.LinqToDsl.DAL.ES.Linq.Visitors
{
    internal static class PartialEvaluator
    {
        private static readonly Type[] DoNotEvaluateMethodsDeclaredOn =
        {
            typeof(Enumerable),
            typeof(Queryable)
        };

        private static readonly ExpressionType[] DoNotEvaluateNodes =
        {
            ExpressionType.Parameter,
            ExpressionType.Lambda
        };

        public static Expression Evaluate(Expression e)
        {
            HashSet<Expression> chosenForEvaluation = BranchSelectExpressionVisitor.Select(e, ShouldEvaluate);
            return EvaluatingExpressionVisitor.Evaluate(e, chosenForEvaluation);
        }

        internal static bool ShouldEvaluate(Expression e)
        {
            bool result = !(DoNotEvaluateNodes.Contains(e.NodeType) ||
                            e is MethodCallExpression mce && DoNotEvaluateMethodsDeclaredOn.Contains(mce.Method.DeclaringType));
            return result;
        }
    }
}
