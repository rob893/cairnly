using System;

namespace Cairnly.API.Models.Entities;

/// <summary>
/// A point-in-time record of an account's balance (in integer minor units) for a single UTC day.
/// At most one snapshot exists per account per day; repeated updates within a day overwrite the
/// existing row (last-write-wins), so the value represents the account's end-of-day balance.
/// </summary>
public sealed class AccountBalanceSnapshot : IIdentifiable<int>, IOwnedByUser<int>, IAuditableEntity
{
    /// <summary>Gets or sets the snapshot ID.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the ID of the owning user.</summary>
    public int UserId { get; set; }

    /// <summary>Gets or sets the owning user navigation property.</summary>
    public User User { get; set; } = default!;

    /// <summary>Gets or sets the ID of the account this snapshot belongs to.</summary>
    public int AccountId { get; set; }

    /// <summary>Gets or sets the account navigation property.</summary>
    public Account Account { get; set; } = default!;

    /// <summary>Gets or sets the UTC day this snapshot represents.</summary>
    public DateOnly AsOf { get; set; }

    /// <summary>Gets or sets the account's resolved balance in integer minor units for the day.</summary>
    public long Balance { get; set; }

    /// <summary>Gets or sets when the snapshot was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Gets or sets when the snapshot was last updated (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Gets or sets the ID of the user who created the snapshot.</summary>
    public int? CreatedById { get; set; }

    /// <summary>Gets or sets the ID of the user who last updated the snapshot.</summary>
    public int? UpdatedById { get; set; }
}