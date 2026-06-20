using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Models.Requests.Accounts;

/// <summary>
/// Request model for fully updating an existing account.
/// </summary>
public sealed record UpdateAccountRequest
{
    /// <summary>Gets the updated account name.</summary>
    [Required]
    [MaxLength(255)]
    public string Name { get; init; } = default!;

    /// <summary>Gets the updated account type.</summary>
    [Required]
    public AccountType Type { get; init; }

    /// <summary>Gets whether the account is an asset or liability.</summary>
    [Required]
    public AccountClass Class { get; init; }

    /// <summary>Gets the ISO 4217 currency code (three letters).</summary>
    [Required]
    [RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "Currency must be a 3-letter ISO 4217 code.")]
    public string Currency { get; init; } = default!;

    /// <summary>Gets the opening balance in integer minor units.</summary>
    public long OpeningBalance { get; init; }

    /// <summary>
    /// Gets the current balance in integer minor units. Only applied when <see cref="IsManual"/> is
    /// <c>true</c>.
    /// </summary>
    public long CurrentBalance { get; init; }

    /// <summary>Gets a value indicating whether the balance is maintained manually.</summary>
    public bool IsManual { get; init; }

    /// <summary>Gets free-form metadata to associate with the account.</summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Creates an <see cref="UpdateAccountRequest"/> from an existing account. Used as the base
    /// document for JSON Patch operations.
    /// </summary>
    /// <param name="account">The account entity.</param>
    /// <returns>A request reflecting the account's current state.</returns>
    public static UpdateAccountRequest FromEntity(Account account)
    {
        ArgumentNullException.ThrowIfNull(account);

        return new UpdateAccountRequest
        {
            Name = account.Name,
            Type = account.Type,
            Class = account.Class,
            Currency = account.Currency,
            OpeningBalance = account.OpeningBalance,
            CurrentBalance = account.CurrentBalance,
            IsManual = account.IsManual,
            Metadata = account.Metadata
        };
    }
}
