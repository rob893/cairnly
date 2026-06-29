using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Data.Repositories;
using Cairnly.API.Models;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Requests;
using Cairnly.API.Services.Auth;
using Cairnly.API.Services.Domain;
using Moq;

namespace Cairnly.API.Tests.Services;

/// <summary>
/// Shared service tests for spendingPlan income and expense line items.
/// </summary>
/// <typeparam name="TEntity">The line item entity type.</typeparam>
/// <typeparam name="TDto">The line item DTO type.</typeparam>
/// <typeparam name="TCreateRequest">The create request type.</typeparam>
/// <typeparam name="TQueryParameters">The query parameter type.</typeparam>
/// <typeparam name="TRepository">The line item repository type.</typeparam>
public abstract class SpendingPlanLineItemServiceTestBase<TEntity, TDto, TCreateRequest, TQueryParameters, TRepository>
    where TEntity : class, ISpendingPlanLineItem
    where TDto : class
    where TCreateRequest : ISpendingPlanLineItemRequest
    where TQueryParameters : CursorPaginationQueryParameters
    where TRepository : class, IRepository<TEntity, TQueryParameters>
{
    /// <summary>The ID of the current test user.</summary>
    protected const int UserId = 42;

    /// <summary>The ID of another test user.</summary>
    protected const int OtherUserId = 99;

    /// <summary>The ID of the spendingPlan under test.</summary>
    protected const int SpendingPlanId = 7;

    /// <summary>Initializes shared mocks for line item service tests.</summary>
    protected SpendingPlanLineItemServiceTestBase()
    {
        this.LineItemRepositoryMock = new Mock<TRepository>();
        this.SpendingPlanRepositoryMock = new Mock<ISpendingPlanRepository>();
        this.ValidatorMock = new Mock<ICategoryTagValidator>();
        this.CurrentUserServiceMock = new Mock<ICurrentUserService>();
        this.CurrentUserServiceMock.Setup(s => s.UserId).Returns(UserId);
        this.CurrentUserServiceMock.Setup(s => s.IsAdmin).Returns(false);
        this.CurrentUserServiceMock
            .Setup(s => s.IsUserAuthorizedForResource(It.IsAny<IOwnedByUser<int>>(), It.IsAny<bool>()))
            .Returns((IOwnedByUser<int> resource, bool _) => resource.UserId == UserId);
        this.ValidatorMock
            .Setup(v => v.ValidateCategoryAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));
        this.ValidatorMock
            .Setup(v => v.ValidateTagsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));
    }

    /// <summary>Gets the line item repository mock.</summary>
    protected Mock<TRepository> LineItemRepositoryMock { get; }

    /// <summary>Gets the spendingPlan repository mock.</summary>
    protected Mock<ISpendingPlanRepository> SpendingPlanRepositoryMock { get; }

    /// <summary>Gets the category and tag validator mock.</summary>
    protected Mock<ICategoryTagValidator> ValidatorMock { get; }

    /// <summary>Gets the current user service mock.</summary>
    protected Mock<ICurrentUserService> CurrentUserServiceMock { get; }

    /// <summary>
    /// Verifies create returns not found when the parent spendingPlan is missing.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task CreateLineItemAsync_SpendingPlanNotFound_ReturnsNotFound()
    {
        this.SpendingPlanRepositoryMock
            .Setup(r => r.GetByIdAsync(SpendingPlanId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SpendingPlan?)null);

        var result = await this.CreateAsync(SpendingPlanId, this.BuildCreateRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.NotFound, result.ErrorType);
    }

    /// <summary>
    /// Verifies delete returns forbidden and does not remove another user's line item.
    /// </summary>
    /// <returns>A task representing the asynchronous test operation.</returns>
    [Fact]
    public async Task DeleteLineItemAsync_OtherUser_ReturnsForbidden()
    {
        this.LineItemRepositoryMock
            .Setup(r => r.GetByIdAsync(1, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(this.BuildLineItem(1, OtherUserId, SpendingPlanId));

        var result = await this.DeleteAsync(SpendingPlanId, 1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Forbidden, result.ErrorType);
        this.LineItemRepositoryMock.Verify(r => r.Remove(It.IsAny<TEntity>()), Times.Never);
    }

    /// <summary>
    /// Creates a line item through the concrete service.
    /// </summary>
    /// <param name="spendingPlanId">The parent spendingPlan ID.</param>
    /// <param name="request">The create request.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The service result.</returns>
    protected abstract Task<Result<TDto>> CreateAsync(int spendingPlanId, TCreateRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a line item through the concrete service.
    /// </summary>
    /// <param name="spendingPlanId">The parent spendingPlan ID.</param>
    /// <param name="lineItemId">The line item ID.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The service result.</returns>
    protected abstract Task<Result<bool>> DeleteAsync(int spendingPlanId, int lineItemId, CancellationToken cancellationToken);

    /// <summary>
    /// Builds a valid create request for the concrete line item type.
    /// </summary>
    /// <returns>The create request.</returns>
    protected abstract TCreateRequest BuildCreateRequest();

    /// <summary>
    /// Builds a line item entity for the concrete line item type.
    /// </summary>
    /// <param name="id">The line item ID.</param>
    /// <param name="userId">The owner user ID.</param>
    /// <param name="spendingPlanId">The parent spendingPlan ID.</param>
    /// <returns>The line item entity.</returns>
    protected abstract TEntity BuildLineItem(int id, int userId, int spendingPlanId);

    /// <summary>
    /// Builds a spendingPlan with the supplied owner.
    /// </summary>
    /// <param name="userId">The owner user ID.</param>
    /// <returns>The spendingPlan entity.</returns>
    protected static SpendingPlan BuildSpendingPlan(int userId)
    {
        return new SpendingPlan { Id = SpendingPlanId, UserId = userId, Name = "SpendingPlan", Currency = "USD" };
    }
}