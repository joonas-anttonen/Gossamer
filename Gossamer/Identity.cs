namespace Gossamer;

/// <summary>
/// A unique identifier. Uniqueness is determined by the <see cref="Id"/>. <see cref="Name"/> can be used for display purposes.
/// </summary>
/// <param name="Id"> The unique identifier of the identity. </param>
/// <param name="Name"> The display name of the identity. </param>
public class Identity(Guid Id, string Name) : IEquatable<Identity>
{
    public Guid Id { get; init; } = Id;

    public string Name { get; set; } = Name;

    /// <summary>
    /// Creates a new <see cref="Identity"/> with a unique identifier and a display name. If no display name is provided, the unique identifier is used.
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static Identity Create(string? name = default)
    {
        Guid id = Guid.NewGuid();
        return new Identity(id, string.IsNullOrEmpty(name) ? id.ToString() : name);
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    public override string ToString() => string.IsNullOrEmpty(Name) ? $"{Id}" : $"{Name}";

    public override bool Equals(object? obj)
    {
        return Equals(obj as Identity);
    }

    public bool Equals(Identity? other)
    {
        return other is not null && Id == other.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}