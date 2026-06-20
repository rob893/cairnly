using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;

namespace Cairnly.API.Data.Repositories;

/// <summary>
/// Repository interface for budget income line item data access.
/// </summary>
public interface IBudgetIncomeRepository : IRepository<BudgetIncome, BudgetIncomeQueryParameters>
{
}
