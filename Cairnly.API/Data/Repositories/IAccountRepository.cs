using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;

namespace Cairnly.API.Data.Repositories;

/// <summary>
/// Repository interface for account data access.
/// </summary>
public interface IAccountRepository : IRepository<Account, AccountQueryParameters>
{
    /// <summary>
    /// Gets the summed transaction amounts (in minor units) for the specified accounts.
    /// </summary>
    /// <param name="accountIds">The account IDs to sum transactions for.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A map of account ID to the sum of its transaction amounts. Accounts with no
    /// transactions are omitted.</returns>
    Task<IReadOnlyDictionary<int, long>> GetTransactionSumsAsync(IEnumerable<int> accountIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the summed transaction amounts (in minor units) for an account, including only
    /// transactions dated on or before the end of the given UTC day.
    /// </summary>
    /// <param name="accountId">The account ID to sum transactions for.</param>
    /// <param name="asOf">The inclusive upper bound (UTC day) on the transaction date.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The sum of the account's transaction amounts up to and including
    /// <paramref name="asOf"/>.</returns>
    Task<long> GetTransactionSumAsOfAsync(int accountId, DateOnly asOf, CancellationToken cancellationToken = default);
}