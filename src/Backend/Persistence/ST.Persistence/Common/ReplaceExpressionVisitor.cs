using System.Linq.Expressions;

namespace ST.Persistence.Common;

internal sealed class ReplaceExpressionVisitor(Expression source, Expression target) : ExpressionVisitor
{
    public override Expression? Visit(Expression? node)
    {
        return node == source
            ? target
            : base.Visit(node);
    }
}
