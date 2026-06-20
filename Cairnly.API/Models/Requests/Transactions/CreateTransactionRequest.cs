using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Models.Requests.Transactions;

/// <summary>
/// Request model for creating a new transaction.
/// </summary>
public sealed record CreateTransactionRequest
{
    /// <summary>Gets the ID of the account the transaction belongs to.</summary>
    [Required]
    public int AccountId { get; init; }

    /// <summary>Gets when the transaction occurred.</summary>
    [Required]
    public DateTimeOffset Date { get; init; }

    /// <summary>Gets the signed amount in integer minor units.</summary>
    [Required]
    public long Amount { get; init; }

    /// <summary>Gets the merchant the transaction was with.</summary>
    [MaxLength(255)]
    public string? Merchant { get; init; }

    /// <summary>Gets an optional free-text description.</summary>
    [MaxLength(1024)]
    public string? Description { get; init; }

    /// <summary>Gets the optional category ID to assign.</summary>
    public int? CategoryId { get; init; }

    /// <summary>Gets how the transaction entered the system. Defaults to manual.</summary>
    public TransactionSource Source { get; init; } = TransactionSource.Manual;

    /// <summary>Gets the optional parent transaction ID when creating a split child.</summary>
    public int? ParentTransactionId { get; init; }

    /// <summary>Gets the IDs of tags to apply to the transaction.</summary>
    public IReadOnlyList<int>? TagIds { get; init; }

    /// <summary>Gets optional free-form metadata to associate with the transaction.</summary>
    public Dictionary<string, object>? Metadata { get; init; }
}
