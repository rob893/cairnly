using System;
using System.Collections.Generic;
using System.Linq;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Models.Dtos;

/// <summary>
/// Data transfer object representing a transaction.
/// </summary>
public sealed record TransactionDto : IIdentifiable<int>, IOwnedByUser<int>
{
    /// <summary>Gets the transaction ID.</summary>
    public required int Id { get; init; }

    /// <summary>Gets the ID of the owning user.</summary>
    public required int UserId { get; init; }

    /// <summary>Gets the ID of the account this transaction belongs to.</summary>
    public required int AccountId { get; init; }

    /// <summary>Gets when the transaction occurred.</summary>
    public required DateTimeOffset Date { get; init; }

    /// <summary>Gets the signed amount in integer minor units.</summary>
    public required long Amount { get; init; }

    /// <summary>Gets the merchant the transaction was with.</summary>
    public string? Merchant { get; init; }

    /// <summary>Gets the free-text description.</summary>
    public string? Description { get; init; }

    /// <summary>Gets the category ID assigned to this transaction.</summary>
    public required int CategoryId { get; init; }

    /// <summary>Gets how the transaction entered the system.</summary>
    public required TransactionSource Source { get; init; }

    /// <summary>Gets a value indicating whether this transaction is split into children.</summary>
    public required bool IsSplit { get; init; }

    /// <summary>Gets the optional parent transaction ID when this is a split child.</summary>
    public int? ParentTransactionId { get; init; }

    /// <summary>Gets the IDs of the tags applied to this transaction.</summary>
    public required IReadOnlyList<int> TagIds { get; init; }

    /// <summary>Gets free-form metadata associated with the transaction.</summary>
    public required Dictionary<string, object> Metadata { get; init; }

    /// <summary>Gets when the transaction record was created.</summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>Gets when the transaction record was last updated.</summary>
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Creates a <see cref="TransactionDto"/> from a <see cref="Transaction"/> entity. The entity's
    /// <see cref="Transaction.TransactionTags"/> must be loaded to populate tag IDs.
    /// </summary>
    /// <param name="transaction">The transaction entity.</param>
    /// <returns>A mapped <see cref="TransactionDto"/>.</returns>
    public static TransactionDto FromEntity(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        return new TransactionDto
        {
            Id = transaction.Id,
            UserId = transaction.UserId,
            AccountId = transaction.AccountId,
            Date = transaction.Date,
            Amount = transaction.Amount,
            Merchant = transaction.Merchant,
            Description = transaction.Description,
            CategoryId = transaction.CategoryId,
            Source = transaction.Source,
            IsSplit = transaction.IsSplit,
            ParentTransactionId = transaction.ParentTransactionId,
            TagIds = transaction.TransactionTags.Select(tt => tt.TagId).ToList(),
            Metadata = transaction.Metadata,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt
        };
    }
}