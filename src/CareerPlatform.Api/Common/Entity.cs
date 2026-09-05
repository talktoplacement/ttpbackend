namespace CareerPlatform.Api.Common;

/// <summary>
/// Base type for domain entities. Provides a single identity value and defines equality
/// based solely on the concrete runtime type and the identifier (Req 9.1). Two entities
/// are equal only when they are the same concrete type and carry the same <see cref="Id"/>.
/// </summary>
/// <typeparam name="TId">The identifier type; must be non-null.</typeparam>
public abstract class Entity<TId>
    where TId : notnull
{
    /// <summary>
    /// The unique identifier for this entity. Assigned once at construction (object initializer or
    /// EF materialization) and immutable thereafter (Req 9.1). <c>init</c> lets EF set store-generated
    /// keys and lets features/seeders/tests supply externally-owned keys (e.g. the Supabase UUID on
    /// <c>UserProfile</c>) without a public setter that would allow post-construction mutation.
    /// </summary>
    public TId Id { get; init; } = default!;

    /// <summary>
    /// Equality by concrete type + <see cref="Id"/> only (Req 9.1). Entities of different
    /// concrete types are never equal, even with matching ids.
    /// </summary>
    public override bool Equals(object? obj) =>
        obj is Entity<TId> other
        && other.GetType() == GetType()
        && EqualityComparer<TId>.Default.Equals(other.Id, Id);

    /// <summary>Hash code derived from the concrete type and <see cref="Id"/> (Req 9.1).</summary>
    public override int GetHashCode() => HashCode.Combine(GetType(), Id);
}
