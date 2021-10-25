using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace GuruOps.ES.LinqToDsl.DAL.ES.Linq.Visitors
{
    internal class BranchSelectExpressionVisitor : ExpressionVisitor
    {
        private readonly HashSet<Expression> _matches = new HashSet<Expression>();
        private readonly Func<Expression, bool> _predicate;
        private bool _decision;

        public BranchSelectExpressionVisitor(Func<Expression, bool> predicate)
        {
            _predicate = predicate;
        }

        public static HashSet<Expression> Select(Expression e, Func<Expression, bool> predicate)
        {
            BranchSelectExpressionVisitor visitor = new BranchSelectExpressionVisitor(predicate);
            visitor.Visit(e);
            return visitor._matches;
        }

        public override Expression Visit(Expression node)
        {
            if (node == null)
                return null;

            bool priorDecision = _decision;
            _decision = false;
            base.Visit(node);

            if (!_decision)
            {
                if (_predicate(node))
                    _matches.Add(node);
                else
                    _decision = true;
            }

            _decision |= priorDecision;
            return node;
        }
    }
}
