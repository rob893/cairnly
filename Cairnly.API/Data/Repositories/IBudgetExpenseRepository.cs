using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;

namespace Cairnly.API.Data.Repositories;

/// <summary>
/// Repository interface for budget expense line item data access.
/// </summary>
public interface IBudgetExpenseRepository : IRepository<BudgetExpense, BudgetExpenseQueryParameters>
{
}
