using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Data.Repositories;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Requests.SpendingPlanExpenses;
using Cairnly.API.Services.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Cairnly.API.Tests.Services;

/// <summary>
/// Tests for <see cref="SpendingPlanExpenseService"/>.
/// </summary>
public sealed class SpendingPlanExpenseServiceTests : SpendingPlanLineItemServiceTestBase<SpendingPlanExpense, SpendingPlanExpenseDto, CreateSpendingPlanExpenseRequest, SpendingPlanExpenseQueryParameters, ISpendingPlanExpenseRepository>
{
    private readonly SpendingPlanExpenseService sut;

    public SpendingPlanExpenseServiceTests()
    {
        this.sut = new SpendingPlanExpenseService(
            NullLogger<SpendingPlanExpenseService>.Instance,
            this.LineItemRepositoryMock.Object,
            this.SpendingPlanRepositoryMock.Object,
            this.ValidatorMock.Object,
            this.CurrentUserServiceMock.Object);
    }

    [Fact]
    public async Task CreateExpenseAsync_Valid_PersistsWithCadence()
    {
        this.SpendingPlanRepositoryMock
            .Setup(r => r.GetByIdAsync(SpendingPlanId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildSpendingPlan(UserId));

        var request = this.BuildCreateRequest() with { Cadence = SpendingPlanCadence.Weekly };
        var result = await this.sut.CreateExpenseAsync(SpendingPlanId, request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(SpendingPlanCadence.Weekly, result.ValueOrThrow.Cadence);
        this.LineItemRepositoryMock.Verify(r => r.Add(It.Is<SpendingPlanExpense>(e => e.UserId == UserId && e.SpendingPlanId == SpendingPlanId)), Times.Once);
    }

    [Fact]
    public async Task CreateExpenseAsync_UnknownTag_ReturnsValidation()
    {
        this.SpendingPlanRepositoryMock
            .Setup(r => r.GetByIdAsync(SpendingPlanId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildSpendingPlan(UserId));
        this.ValidatorMock
            .Setup(v => v.ValidateTagsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Failure(DomainErrorType.Validation, "One or more of the specified tags do not exist"));

        var request = this.BuildCreateRequest() with { TagIds = new[] { 3 } };
        var result = await this.sut.CreateExpenseAsync(SpendingPlanId, request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Validation, result.ErrorType);
    }

    /// <inheritdoc />
    protected override Task<Result<SpendingPlanExpenseDto>> CreateAsync(int spendingPlanId, CreateSpendingPlanExpenseRequest request, CancellationToken cancellationToken)
    {
        return this.sut.CreateExpenseAsync(spendingPlanId, request, cancellationToken);
    }

    /// <inheritdoc />
    protected override Task<Result<bool>> DeleteAsync(int spendingPlanId, int lineItemId, CancellationToken cancellationToken)
    {
        return this.sut.DeleteExpenseAsync(spendingPlanId, lineItemId, cancellationToken);
    }

    /// <inheritdoc />
    protected override CreateSpendingPlanExpenseRequest BuildCreateRequest()
    {
        return new CreateSpendingPlanExpenseRequest { Name = "Rent", Amount = 200000, Cadence = SpendingPlanCadence.Monthly };
    }

    /// <inheritdoc />
    protected override SpendingPlanExpense BuildLineItem(int id, int userId, int spendingPlanId)
    {
        return new SpendingPlanExpense { Id = id, UserId = userId, SpendingPlanId = spendingPlanId };
    }
}