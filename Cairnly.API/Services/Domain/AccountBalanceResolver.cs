using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Data.Repositories;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Resolves account balances from the register model (opening balance plus the sum of
/// transactions). Shared by <see cref="AccountService"/> and <see cref="BalanceHistoryService"/>.
/// </summary>
public sealed class AccountBalanceResolver : IAccountBalanceResolver
{
    private readonly IAccountRepository accountRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="AccountBalanceResolver"/> class.
    /// </summary>
    /// <param name="accountRepository">The account repository.</param>
    public AccountBalanceResolver(IAccountRepository accountRepository)
    {
        this.accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyDictionary<int, long>> ResolveBalancesAsync(IReadOnlyList<Account> accounts, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accounts);

        var ids = accounts.Select(a => a.Id).ToList();

        var sums = ids.Count > 0
            ? await this.accountRepository.GetTransactionSumsAsync(ids, cancellationToken)
            : new Dictionary<int, long>();

        var balances = new Dictionary<int, long>(accounts.Count);

        foreach (var account in accounts)
        {
            balances[account.Id] = account.OpeningBalance + (sums.TryGetValue(account.Id, out var sum) ? sum : 0);
        }

        return balances;
    }

    /// <inheritdoc />
    public async Task<long> ResolveBalanceAsOfAsync(Account account, DateOnly asOf, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(account);

        var sum = await this.accountRepository.GetTransactionSumAsOfAsync(account.Id, asOf, cancellationToken);

        return account.OpeningBalance + sum;
    }
}