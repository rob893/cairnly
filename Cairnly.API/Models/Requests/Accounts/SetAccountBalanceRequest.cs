using System;
using System.ComponentModel.DataAnnotations;

namespace Cairnly.API.Models.Requests.Accounts;

/// <summary>
/// Request model for setting an account's balance to a specific value as of a given day. The server
/// records a balance-adjustment transaction for the difference between the target and the resolved
/// balance on that day.
/// </summary>
public sealed record SetAccountBalanceRequest
{
    /// <summary>Gets the UTC day the balance should be set as of.</summary>
    [Required]
    public DateOnly AsOf { get; init; }

    /// <summary>Gets the target balance in integer minor units.</summary>
    [Required]
    public long Balance { get; init; }
}
