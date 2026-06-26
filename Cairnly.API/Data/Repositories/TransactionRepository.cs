using System.Linq;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;
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
            query = query.Where(t => t.Date >= searchParams.DateFrom.Value);
        }

        if (searchParams.DateTo.HasValue)
        {
            query = query.Where(t => t.Date <= searchParams.DateTo.Value);
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