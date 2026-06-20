using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Models.Requests.Accounts;

/// <summary>
/// Request model for creating a new account.
/// </summary>
public sealed record CreateAccountRequest
{
    /// <summary>Gets the account name.</summary>
    [Required]
    [MaxLength(255)]
    public string Name { get; init; } = default!;

    /// <summary>Gets the account type.</summary>
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

    /// <summary>
    /// Gets a value indicating whether the balance is maintained manually. Defaults to <c>false</c>
    /// (balance derived from transactions).
    /// </summary>
    public bool IsManual { get; init; }

    /// <summary>Gets optional free-form metadata to associate with the account.</summary>
    public Dictionary<string, object>? Metadata { get; init; }
}
