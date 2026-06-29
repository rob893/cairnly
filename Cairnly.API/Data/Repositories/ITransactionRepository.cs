using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Reports;

namespace Cairnly.API.Data.Repositories;

/// <summary>
/// Repository interface for transaction data access.
/// </summary>
public interface ITransactionRepository : IRepository<Transaction, TransactionQueryParameters>
{
    /// <summary>
    /// Gets the flattened, signed transactions used to build the cash-flow report for a user.
    /// Excludes split children, balance adjustments, and transfers, and projects each row with its
    /// category and parent-group metadata.
    /// </summary>
    /// <param name="userId">The owning user ID.</param>
    /// <param name="from">An optional inclusive lower bound on the transaction date.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The cash-flow entries ordered by date ascending.</returns>
    Task<IReadOnlyList<CashFlowEntry>> GetCashFlowEntriesAsync(int userId, DateTimeOffset? from, CancellationToken cancellationToken = default);
}