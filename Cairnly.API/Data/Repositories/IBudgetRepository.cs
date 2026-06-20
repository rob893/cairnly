using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;

namespace Cairnly.API.Data.Repositories;

/// <summary>
/// Repository interface for budget data access.
/// </summary>
public interface IBudgetRepository : IRepository<Budget, BudgetQueryParameters>
{
}
