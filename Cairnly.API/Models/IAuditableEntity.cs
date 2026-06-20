using System;

namespace Cairnly.API.Models;

/// <summary>
/// Marks an entity that tracks common audit timestamps. These are stamped automatically by the
/// <see cref="Data.DataContext"/> on save.
/// </summary>
public interface IAuditableEntity
{
    /// <summary>Gets or sets when the entity was created (UTC).</summary>
    DateTimeOffset CreatedAt { get; set; }

    /// <summary>Gets or sets when the entity was last updated (UTC).</summary>
    DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who created the entity. <c>null</c> when created by a
    /// non-user/system process (e.g. seeding).
    /// </summary>
    int? CreatedById { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user who last updated the entity. <c>null</c> when last modified by
    /// a non-user/system process.
    /// </summary>
    int? UpdatedById { get; set; }
}
