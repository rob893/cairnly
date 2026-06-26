using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Models.Requests.Transactions;

/// <summary>
/// Request model for fully updating an existing transaction.
/// </summary>
public sealed record UpdateTransactionRequest
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

    /// <summary>Gets the category ID to assign.</summary>
    [Required]
    public int CategoryId { get; init; }

    /// <summary>Gets how the transaction entered the system.</summary>
    public TransactionSource Source { get; init; } = TransactionSource.Manual;

    /// <summary>Gets the IDs of tags to apply to the transaction (replaces the existing set).</summary>
    public IReadOnlyList<int>? TagIds { get; init; }

    /// <summary>Gets free-form metadata to associate with the transaction.</summary>
    public Dictionary<string, object>? Metadata { get; init; }

    /// <summary>
    /// Creates an <see cref="UpdateTransactionRequest"/> from an existing transaction. The entity's
    /// <see cref="Transaction.TransactionTags"/> must be loaded to populate tag IDs. Used as the base
    /// document for JSON Patch operations.
    /// </summary>
    /// <param name="transaction">The transaction entity.</param>
    /// <returns>A request reflecting the transaction's current state.</returns>
    public static UpdateTransactionRequest FromEntity(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        return new UpdateTransactionRequest
        {
            AccountId = transaction.AccountId,
            Date = transaction.Date,
            Amount = transaction.Amount,
            Merchant = transaction.Merchant,
            Description = transaction.Description,
            CategoryId = transaction.CategoryId,
            Source = transaction.Source,
            TagIds = transaction.TransactionTags.Select(tt => tt.TagId).ToList(),
            Metadata = transaction.Metadata
        };
    }
}