using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Reports;
using Microsoft.EntityFrameworkCore;

namespace Cairnly.API.Data.Repositories;

/// <summary>
/// Repository for transaction data access.
/// </summary>
public sealed class TransactionRepository : Repository<Transaction, TransactionQueryParameters>, ITransactionRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public TransactionRepository(DataContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CashFlowEntry>> GetCashFlowEntriesAsync(int userId, DateTimeOffset? from, CancellationToken cancellationToken = default)
    {
        var query = this.Context.Transactions
            .AsNoTracking()
            .Where(t => t.UserId == userId
                && t.ParentTransactionId == null
                && !t.IsBalanceAdjustment
                && t.Category.Kind != CategoryKind.Transfer);

        if (from.HasValue)
        {
            query = query.Where(t => t.Date >= from.Value);
        }

        return await query
            .OrderBy(t => t.Date)
            .Select(t => new CashFlowEntry
            {
                Date = t.Date,
                Amount = t.Amount,
                Kind = t.Category.Kind,
                CategoryId = t.CategoryId,
                CategoryName = t.Category.Name,
                CategoryIcon = t.Category.Icon,
                ParentCategoryId = t.Category.ParentId,
                ParentName = t.Category.Parent != null ? t.Category.Parent.Name : null,
                ParentIcon = t.Category.Parent != null ? t.Category.Parent.Icon : null,
                Merchant = t.Merchant
            })
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    protected override IQueryable<Transaction> AddIncludes(IQueryable<Transaction> query)
    {
        return query.Include(t => t.TransactionTags);
    }

    /// <inheritdoc />
    protected override IQueryable<Transaction> AddWhereClauses(IQueryable<Transaction> query, TransactionQueryParameters searchParams)
    {
        if (!searchParams.RequestingUserIsAdmin)
        {
            query = query.Where(t => t.UserId == searchParams.RequestingUserId);
        }

        if (searchParams.ParentTransactionId.HasValue)
        {
            query = query.Where(t => t.ParentTransactionId == searchParams.ParentTransactionId.Value);
        }
        else if (!searchParams.IncludeSplitChildren)
        {
            query = query.Where(t => t.ParentTransactionId == null);
        }

        if (searchParams.AccountId.HasValue)
        {
            query = query.Where(t => t.AccountId == searchParams.AccountId.Value);
        }

        if (searchParams.CategoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == searchParams.CategoryId.Value);
        }

        if (searchParams.CategoryIds is { Count: > 0 } categoryIds)
        {
            query = query.Where(t => categoryIds.Contains(t.CategoryId));
        }

        if (searchParams.TagId.HasValue)
        {
            query = query.Where(t => t.TransactionTags.Any(tt => tt.TagId == searchParams.TagId.Value));
        }

        if (searchParams.Source.HasValue)
        {
            query = query.Where(t => t.Source == searchParams.Source.Value);
        }

        if (searchParams.DateFrom.HasValue)
        {
            var dateFrom = searchParams.DateFrom.Value.ToUniversalTime();
            query = query.Where(t => t.Date >= dateFrom);
        }

        if (searchParams.DateTo.HasValue)
        {
            var dateTo = searchParams.DateTo.Value.ToUniversalTime();
            query = query.Where(t => t.Date <= dateTo);
        }

        if (searchParams.MinAmount.HasValue)
        {
            query = query.Where(t => t.Amount >= searchParams.MinAmount.Value);
        }

        if (searchParams.MaxAmount.HasValue)
        {
            query = query.Where(t => t.Amount <= searchParams.MaxAmount.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchParams.Merchant))
        {
            query = query.Where(t => t.Merchant != null && EF.Functions.ILike(t.Merchant, $"%{searchParams.Merchant}%"));
        }

        return query;
    }
}