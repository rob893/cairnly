using System;
using System.Linq;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;
using Microsoft.EntityFrameworkCore;

namespace Cairnly.API.Data.Repositories;

/// <summary>
/// Base repository for spendingPlan line item data access.
/// </summary>
/// <typeparam name="TEntity">The line item entity type.</typeparam>
/// <typeparam name="TSearchParams">The line item search parameter type.</typeparam>
public abstract class SpendingPlanLineItemRepository<TEntity, TSearchParams> : Repository<TEntity, TSearchParams>
    where TEntity : class, ISpendingPlanLineItem
    where TSearchParams : OwnedEntityQueryParameters, ISpendingPlanLineItemQueryParameters
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SpendingPlanLineItemRepository{TEntity,TSearchParams}"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    protected SpendingPlanLineItemRepository(DataContext context) : base(context)
    {
    }

    /// <inheritdoc />
    protected override IQueryable<TEntity> AddWhereClauses(IQueryable<TEntity> query, TSearchParams searchParams)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(searchParams);

        query = query.Where(e => EF.Property<int>(e, nameof(ISpendingPlanLineItem.SpendingPlanId)) == searchParams.SpendingPlanId);

        if (!searchParams.RequestingUserIsAdmin)
        {
            query = query.Where(e => EF.Property<int>(e, nameof(ISpendingPlanLineItem.UserId)) == searchParams.RequestingUserId);
        }

        if (!string.IsNullOrWhiteSpace(searchParams.Name))
        {
            query = query.Where(e => EF.Functions.ILike(EF.Property<string>(e, nameof(ISpendingPlanLineItem.Name)), $"%{searchParams.Name}%"));
        }

        return this.AddLineItemWhereClauses(query, searchParams);
    }

    /// <summary>
    /// Adds entity-specific filters to the shared line item query.
    /// </summary>
    /// <param name="query">The query being filtered.</param>
    /// <param name="searchParams">The search parameters.</param>
    /// <returns>The filtered query.</returns>
    protected virtual IQueryable<TEntity> AddLineItemWhereClauses(IQueryable<TEntity> query, TSearchParams searchParams)
    {
        return query;
    }
}