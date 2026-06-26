using System;
using System.Collections.Generic;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Models.Dtos;

/// <summary>
/// Data transfer object representing a financial account.
/// </summary>
public sealed record AccountDto : IIdentifiable<int>, IOwnedByUser<int>
{
    /// <summary>Gets the account ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the ID of the owning user.</summary>
    public required int UserId { get; init; }

    /// <summary>Gets the account name.</summary>
    public required string Name { get; init; }

    /// <summary>Gets the account type.</summary>
    public required AccountType Type { get; init; }

    /// <summary>Gets whether the account is an asset or liability.</summary>
    public required AccountClass Class { get; init; }

    /// <summary>Gets the ISO 4217 currency code.</summary>
    public required string Currency { get; init; }

    /// <summary>Gets the opening balance in integer minor units.</summary>
    public required long OpeningBalance { get; init; }

    /// <summary>
    /// Gets the current balance in integer minor units. Derived from transactions unless the
    /// account is manual, in which case the stored value is returned.
    /// </summary>
    public required long CurrentBalance { get; init; }

    /// <summary>Gets a value indicating whether the balance is maintained manually.</summary>
    public required bool IsManual { get; init; }

    /// <summary>Gets free-form metadata associated with the account.</summary>
    public required Dictionary<string, object> Metadata { get; init; }

    /// <summary>Gets when the account was created.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Gets when the account was last updated.</summary>
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Creates an <see cref="AccountDto"/> from an <see cref="Account"/> entity.
    /// </summary>
    /// <param name="account">The account entity.</param>
    /// <param name="currentBalance">The resolved current balance in minor units.</param>
    /// <returns>A mapped <see cref="AccountDto"/>.</returns>
    public static AccountDto FromEntity(Account account, long currentBalance)
    {
        ArgumentNullException.ThrowIfNull(account);

        return new AccountDto
        {
            Id = account.Id,
            UserId = account.UserId,
            Name = account.Name,
            Type = account.Type,
            Class = account.Class,
            Currency = account.Currency,
            OpeningBalance = account.OpeningBalance,
            CurrentBalance = currentBalance,
            IsManual = account.IsManual,
            Metadata = account.Metadata,
            CreatedAt = account.CreatedAt,
            UpdatedAt = account.UpdatedAt
        };
    }
}