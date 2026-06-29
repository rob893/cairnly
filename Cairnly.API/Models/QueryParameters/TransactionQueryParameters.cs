using System;
using System.Collections.Generic;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Models.QueryParameters;

/// <summary>
/// Query parameters for searching transactions.
/// </summary>
public sealed record TransactionQueryParameters : OwnedEntityQueryParameters
{
    /// <summary>Gets an optional account ID filter.</summary>
    public int? AccountId { get; init; }

    /// <summary>Gets an optional category ID filter.</summary>
    public int? CategoryId { get; init; }

    /// <summary>
    /// Gets an optional set of category IDs to filter by. When non-empty, returns transactions in
    /// any of these categories (used to drill into a category group's children).
    /// </summary>
    public IReadOnlyCollection<int>? CategoryIds { get; init; }

    /// <summary>Gets an optional tag ID filter.</summary>
    public int? TagId { get; init; }

    /// <summary>Gets an optional source filter.</summary>
    public TransactionSource? Source { get; init; }

    /// <summary>Gets an optional inclusive lower bound on the transaction date.</summary>
    public DateTimeOffset? DateFrom { get; init; }

    /// <summary>Gets an optional inclusive upper bound on the transaction date.</summary>
    public DateTimeOffset? DateTo { get; init; }

    /// <summary>Gets an optional inclusive lower bound on the amount (minor units).</summary>
    public long? MinAmount { get; init; }

    /// <summary>Gets an optional inclusive upper bound on the amount (minor units).</summary>
    public long? MaxAmount { get; init; }

    /// <summary>Gets an optional merchant filter (case-insensitive substring match).</summary>
    public string? Merchant { get; init; }

    /// <summary>
    /// Gets an optional parent transaction ID filter. When set, returns the split children of that
    /// transaction.
    /// </summary>
    public int? ParentTransactionId { get; init; }

    /// <summary>
    /// Gets a value indicating whether to include split child transactions in the results. Defaults
    /// to <c>false</c> (only top-level transactions are returned). Ignored when
    /// <see cref="ParentTransactionId"/> is specified.
    /// </summary>
    public bool IncludeSplitChildren { get; init; }
}