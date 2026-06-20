using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;

namespace Cairnly.API.Data.Repositories;

/// <summary>
/// Repository interface for transaction data access.
/// </summary>
public interface ITransactionRepository : IRepository<Transaction, TransactionQueryParameters>
{
}
