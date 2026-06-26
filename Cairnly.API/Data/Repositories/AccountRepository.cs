using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;
using Microsoft.EntityFrameworkCore;

namespace Cairnly.API.Data.Repositories;

/// <summary>
/// Repository for account data access.
/// </summary>
public sealed class AccountRepository : Repository<Account, AccountQueryParameters>, IAccountRepository
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountRepository"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public AccountRepository(DataContext context) : base(context)
    {
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<int, long>> GetTransactionSumsAsync(IEnumerable<int> accountIds, CancellationToken cancellationToken = default)
    {
        var ids = accountIds as IReadOnlyCollection<int> ?? accountIds.ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<int, long>();
        }

        var sums = await this.Context.Transactions
            .AsNoTracking()
            .Where(t => ids.Contains(t.AccountId) && t.ParentTransactionId == null)
            .GroupBy(t => t.AccountId)
            .Select(g => new { AccountId = g.Key, Sum = g.Sum(t => t.Amount) })
            .ToListAsync(cancellationToken);

        return sums.ToDictionary(s => s.AccountId, s => s.Sum);
    }

    /// <inheritdoc />
    protected override IQueryable<Account> AddWhereClauses(IQueryable<Account> query, AccountQueryParameters searchParams)
    {
        if (!searchParams.RequestingUserIsAdmin)
        {
            query = query.Where(a => a.UserId == searchParams.RequestingUserId);
        }

        if (searchParams.Type.HasValue)
        {
            query = query.Where(a => a.Type == searchParams.Type.Value);
        }

        if (searchParams.Class.HasValue)
        {
            query = query.Where(a => a.Class == searchParams.Class.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchParams.Currency))
        {
            var currency = searchParams.Currency.ToUpperInvariant();
            query = query.Where(a => a.Currency == currency);
        }

        if (searchParams.IsManual.HasValue)
        {
            query = query.Where(a => a.IsManual == searchParams.IsManual.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchParams.Name))
        {
            query = query.Where(a => EF.Functions.ILike(a.Name, $"%{searchParams.Name}%"));
        }

        return query;
    }
}