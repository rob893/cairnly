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
}
