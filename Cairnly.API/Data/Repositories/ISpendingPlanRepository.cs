using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;

namespace Cairnly.API.Data.Repositories;

/// <summary>
/// Repository interface for spendingPlan data access.
/// </summary>
public interface ISpendingPlanRepository : IRepository<SpendingPlan, SpendingPlanQueryParameters>
{
}
