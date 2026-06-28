using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Resolves account balances from the register model: an account's balance is its opening balance
/// plus the sum of its (top-level) transactions, including balance-adjustment transactions.
/// </summary>
public interface IAccountBalanceResolver
{
    /// <summary>
    /// Resolves the current balance (in minor units) for each of the given accounts in a single
    /// batched query.
    /// </summary>
    /// <param name="accounts">The accounts to resolve balances for.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A map of account ID to current balance.</returns>
    Task<IReadOnlyDictionary<int, long>> ResolveBalancesAsync(IReadOnlyList<Account> accounts, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves an account's balance (in minor units) as of the end of the given UTC day: its
    /// opening balance plus the sum of transactions dated on or before that day.
    /// </summary>
    /// <param name="account">The account to resolve the balance for.</param>
    /// <param name="asOf">The UTC day to resolve the balance as of.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The account's balance as of <paramref name="asOf"/>.</returns>
    Task<long> ResolveBalanceAsOfAsync(Account account, DateOnly asOf, CancellationToken cancellationToken);
}
