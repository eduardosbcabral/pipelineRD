using System.Linq.Expressions;

namespace PipelineRD.Internals;

internal struct ExpressionCacheKey : IEquatable<ExpressionCacheKey>
{
    private readonly int _hashCode;

    public ExpressionCacheKey(Expression expression)
    {
        _hashCode = ExpressionEqualityComparer.Instance.GetHashCode(expression);
    }

    public bool Equals(ExpressionCacheKey other)
    {
        return _hashCode == other._hashCode;
    }

    public override int GetHashCode()
    {
        return _hashCode;
    }
}
