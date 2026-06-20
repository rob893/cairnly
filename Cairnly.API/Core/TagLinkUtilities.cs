using System;
using System.Collections.Generic;
using System.Linq;
using Cairnly.API.Models;

namespace Cairnly.API.Core;

/// <summary>
/// Utility methods for working with requested tag IDs and tag-link join entities.
/// </summary>
internal static class TagLinkUtilities
{
    /// <summary>
    /// Normalizes a nullable list of requested tag IDs into a distinct list (empty when null).
    /// </summary>
    /// <param name="tagIds">The requested tag IDs.</param>
    /// <returns>A distinct list of tag IDs.</returns>
    public static List<int> Normalize(IReadOnlyList<int>? tagIds)
    {
        return tagIds == null ? [] : tagIds.Distinct().ToList();
    }

    /// <summary>
    /// Synchronizes a collection of tag-link join entities to match the desired set of tag IDs,
    /// removing links no longer wanted and adding new ones via the supplied factory.
    /// </summary>
    /// <typeparam name="TLink">The tag-link join entity type.</typeparam>
    /// <param name="links">The existing tag-link collection (mutated in place).</param>
    /// <param name="desiredTagIds">The desired set of tag IDs.</param>
    /// <param name="createLink">Factory creating a new link for a tag ID (sets the owning FK).</param>
    public static void Sync<TLink>(List<TLink> links, IReadOnlyCollection<int> desiredTagIds, Func<int, TLink> createLink)
        where TLink : ITagLink
    {
        ArgumentNullException.ThrowIfNull(links);
        ArgumentNullException.ThrowIfNull(desiredTagIds);
        ArgumentNullException.ThrowIfNull(createLink);

        var desired = desiredTagIds.ToHashSet();
        var existing = links.Select(l => l.TagId).ToHashSet();

        links.RemoveAll(l => !desired.Contains(l.TagId));

        foreach (var tagId in desired.Where(tagId => !existing.Contains(tagId)))
        {
            links.Add(createLink(tagId));
        }
    }
}
