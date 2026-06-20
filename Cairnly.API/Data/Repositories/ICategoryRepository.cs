using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;

namespace Cairnly.API.Data.Repositories;

/// <summary>
/// Repository interface for category data access.
/// </summary>
public interface ICategoryRepository : IRepository<Category, CategoryQueryParameters>
{
}
