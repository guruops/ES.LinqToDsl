using System;
using System.Linq.Expressions;

namespace GuruOps.ES.LinqToDsl.DAL.ES.Linq.Visitors
{
    public class GuardianVisitor : ExpressionVisitor
    {
        private const string SayNoGoto = "To show respect to Dijkstra we don't support GoTo.";

        protected sealed override Expression VisitLambda<T>(Expression<T> node)
        {
            throw new NotSupportedException($"Sorry, nested lambda has not yet been supported. Expression is {node}.");
        }

        protected sealed override Expression VisitIndex(IndexExpression node)
        {
            throw Create(node);
        }

        protected sealed override MemberListBinding VisitMemberListBinding(MemberListBinding node)
        {
            throw Create(node);
        }

        protected sealed override Expression VisitTypeBinary(TypeBinaryExpression node)
        {
            throw Create(node);
        }

        protected sealed override MemberAssignment VisitMemberAssignment(MemberAssignment node)
        {
            throw Create(node);
        }

        protected sealed override Expression VisitInvocation(InvocationExpression node)
        {
            throw Create(node);
        }

        protected sealed override MemberBinding VisitMemberBinding(MemberBinding node)
        {
            throw Create(node);
        }

        protected sealed override MemberMemberBinding VisitMemberMemberBinding(MemberMemberBinding node)
        {
            throw Create(node);
        }

        protected sealed override Expression VisitMemberInit(MemberInitExpression node)
        {
            throw Create(node);
        }

        protected sealed override Expression VisitBlock(BlockExpression node)
        {
            throw Create(node);
        }

        protected sealed override Expression VisitConditional(ConditionalExpression node)
        {
            throw Create(node);
        }

        protected sealed override Expression VisitTry(TryExpression node)
        {
            throw Create(node);
        }

        protected sealed override Expression VisitParameter(ParameterExpression node)
        {
            throw Create(node);
        }

        protected sealed override Expression VisitConstant(ConstantExpression node)
        {
            return node;
        }

        protected sealed override Expression VisitLoop(LoopExpression node)
        {
            throw Create(node);
        }

        protected sealed override Expression VisitNew(NewExpression node)
        {
            throw Create(node);
        }

        protected sealed override Expression VisitNewArray(NewArrayExpression node)
        {
            throw Create(node);
        }

        protected sealed override Expression VisitListInit(ListInitExpression node)
        {
            throw Create(node);
        }

        protected sealed override SwitchCase VisitSwitchCase(SwitchCase node)
        {
            throw Create(node);
        }

        protected sealed override ElementInit VisitElementInit(ElementInit node)
        {
            throw Create(node);
        }

        protected sealed override CatchBlock VisitCatchBlock(CatchBlock node)
        {
            throw Create(node);
        }

        protected sealed override Expression VisitSwitch(SwitchExpression node)
        {
            throw Create(node);
        }

        protected sealed override Expression VisitDynamic(DynamicExpression node)
        {
            throw Create(node);
        }

        protected sealed override Expression VisitRuntimeVariables(RuntimeVariablesExpression node)
        {
            throw Create(node);
        }

        protected sealed override Expression VisitExtension(Expression node)
        {
            throw Create(node);
        }

        protected sealed override Expression VisitDefault(DefaultExpression node)
        {
            throw Create(node);
        }

        protected sealed override Expression VisitGoto(GotoExpression node)
        {
            throw new NotSupportedException(SayNoGoto);
        }

        protected sealed override Expression VisitLabel(LabelExpression node)
        {
            throw new NotSupportedException(SayNoGoto);
        }

        protected sealed override LabelTarget VisitLabelTarget(LabelTarget node)
        {
            throw new NotSupportedException(SayNoGoto);
        }

        private static NotSupportedException Create(object node)
        {
            throw new NotSupportedException($"Node - {node.GetType().Name} is not supported. Expression is {node}.");
        }

        private static NotSupportedException Create(Expression node)
        {
            throw new NotSupportedException($"Expression - {node.GetType().Name} is not supported. Expression is {node}.");
        }

        private static NotSupportedException Create(MemberBinding binding)
        {
            throw new NotSupportedException($"Binding - {binding.GetType().Name} is not supported. Binding is {binding}.");
        }
    }
}
