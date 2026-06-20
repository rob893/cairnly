using System;

namespace Cairnly.API.Models;

public interface IOwnedByUser<TKey> where TKey : IEquatable<TKey>, IComparable<TKey>
{
    TKey UserId { get; }
}