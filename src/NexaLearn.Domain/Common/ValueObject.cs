namespace NexaLearn.Domain.Common;

public abstract class ValueObject
{
    protected abstract IEnumerable<object> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;
        return GetEqualityComponents()
            .SequenceEqual(((ValueObject)obj).GetEqualityComponents());
    }

    public override int GetHashCode() =>
        GetEqualityComponents()
            .Aggregate(0, (hash, component) => HashCode.Combine(hash, component));

    /// <remarks>
    /// Los cuatro casos de null se manejan explícitamente porque <c>==</c> sobre tipos de
    /// referencia sin sobrecarga usa igualdad de referencia, no estructural. Sin este manejo:
    /// <list type="bullet">
    ///   <item><c>null == null</c> retornaría <c>false</c> al intentar llamar <see cref="Equals"/> sobre null.</item>
    ///   <item><c>value == null</c> dispararía una <see cref="NullReferenceException"/>.</item>
    /// </list>
    /// </remarks>
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (left is null && right is null) return true;
        if (left is null || right is null) return false;
        return left.Equals(right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right) =>
        !(left == right);
}
