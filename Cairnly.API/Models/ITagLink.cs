namespace Cairnly.API.Models;

/// <summary>
/// A join entity that links a taggable entity to a tag. Enables generic tag-link synchronization.
/// </summary>
public interface ITagLink
{
    /// <summary>Gets the ID of the linked tag.</summary>
    int TagId { get; }
}