using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cairnly.API.Models.Entities;

/// <summary>
/// Represents a financial account (asset or liability) owned by a user.
/// </summary>
public sealed class Account : IIdentifiable<int>, IOwnedByUser<int>, IAuditableEntity
{
    /// <summary>Gets or sets the account ID.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the ID of the owning user.</summary>
    public int UserId { get; set; }

    /// <summary>Gets or sets the owning user navigation property.</summary>
    public User User { get; set; } = default!;

    /// <summary>Gets or sets the account name.</summary>
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the account type.</summary>
    public AccountType Type { get; set; }

    /// <summary>Gets or sets whether the account is an asset or liability.</summary>
    public AccountClass Class { get; set; }

    /// <summary>Gets or sets the ISO 4217 currency code (e.g. "USD").</summary>
    [MaxLength(3)]
    public string Currency { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the opening balance in integer minor units. Used as the starting point when the
    /// balance is derived from transactions.
    /// </summary>
    public long OpeningBalance { get; set; }

    /// <summary>
    /// Gets or sets the manually maintained current balance in integer minor units. Only meaningful
    /// when <see cref="IsManual"/> is <c>true</c>.
    /// </summary>
    public long CurrentBalance { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the balance is maintained manually. When
    /// <c>false</c> (default) the balance is derived from <see cref="OpeningBalance"/> plus the sum
    /// of the account's transactions.
    /// </summary>
    public bool IsManual { get; set; }

    /// <summary>
    /// Gets or sets free-form metadata stored as jsonb for future-proofing.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = [];

    /// <summary>Gets or sets when the account was created (UTC).</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Gets or sets when the account was last updated (UTC).</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Gets or sets the ID of the user who created the account.</summary>
    public int? CreatedById { get; set; }

    /// <summary>Gets or sets the ID of the user who last updated the account.</summary>
    public int? UpdatedById { get; set; }

    /// <summary>Gets or sets the transactions belonging to this account.</summary>
    public List<Transaction> Transactions { get; set; } = [];
}