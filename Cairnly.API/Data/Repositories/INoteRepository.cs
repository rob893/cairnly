using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;

namespace Cairnly.API.Data.Repositories;

/// <summary>
/// Repository interface for note data access.
/// </summary>
public interface INoteRepository : IRepository<Note, NoteQueryParameters>
{
}
