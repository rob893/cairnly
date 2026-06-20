using System;

namespace Cairnly.API.Models.Entities;

/// <summary>
/// Per-user application preferences (1:1 with a user). The actual preference values live in the
/// jsonb <see cref="Data"/> document so new preferences can be added without schema changes.
/// </summary>
public sealed class UserPreferences : IIdentifiable<int>, IOwnedByUser<int>, IAuditableEntity
{
    /// <summary>Gets or sets the preferences row ID.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the ID of the owning user.</summary>
    public int UserId { get; set; }

    /// <summary>Gets or sets the owning user navigation property.</summary>
    public User User { get; set; } = default!;

    /// <summary>Gets or sets the preference document (stored as jsonb).</summary>
    public UserPreferencesData Data { get; set; } = new();

    /// <summary>Gets or sets when the preferences were created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Gets or sets when the preferences were last updated (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Gets or sets the ID of the user who created the preferences.</summary>
    public int? CreatedById { get; set; }

    /// <summary>Gets or sets the ID of the user who last updated the preferences.</summary>
    public int? UpdatedById { get; set; }
}
