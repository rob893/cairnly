using System;

namespace Cairnly.API.Models;

public interface IIdentifiable<TKey> where TKey : IEquatable<TKey>, IComparable<TKey>
{
    TKey Id { get; }
}