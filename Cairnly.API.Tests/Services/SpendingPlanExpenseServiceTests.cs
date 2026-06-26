using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Data.Repositories;
using Cairnly.API.Models;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.Requests.SpendingPlanExpenses;
using Cairnly.API.Services.Auth;
using Cairnly.API.Services.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Cairnly.API.Tests.Services;

/// <summary>
/// Tests for <see cref="SpendingPlanExpenseService"/>.
/// </summary>
public sealed class SpendingPlanExpenseServiceTests
{
    private const int UserId = 42;
    private const int OtherUserId = 99;
    private const int SpendingPlanId = 7;

    private readonly Mock<ISpendingPlanExpenseRepository> expenseRepositoryMock;
    private readonly Mock<ISpendingPlanRepository> spendingPlanRepositoryMock;
    private readonly Mock<ICategoryTagValidator> validatorMock;
    private readonly Mock<ICurrentUserService> currentUserServiceMock;
    private readonly SpendingPlanExpenseService sut;

    public SpendingPlanExpenseServiceTests()
    {
        this.expenseRepositoryMock = new Mock<ISpendingPlanExpenseRepository>();
        this.spendingPlanRepositoryMock = new Mock<ISpendingPlanRepository>();
        this.validatorMock = new Mock<ICategoryTagValidator>();
        this.currentUserServiceMock = new Mock<ICurrentUserService>();
        this.currentUserServiceMock.Setup(s => s.UserId).Returns(UserId);
        this.currentUserServiceMock.Setup(s => s.IsAdmin).Returns(false);
        this.currentUserServiceMock
            .Setup(s => s.IsUserAuthorizedForResource(It.IsAny<IOwnedByUser<int>>(), It.IsAny<bool>()))
            .Returns((IOwnedByUser<int> resource, bool _) => resource.UserId == UserId);
        this.validatorMock
            .Setup(v => v.ValidateCategoryAsync(It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));
        this.validatorMock
            .Setup(v => v.ValidateTagsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        this.sut = new SpendingPlanExpenseService(
            NullLogger<SpendingPlanExpenseService>.Instance,
            this.expenseRepositoryMock.Object,
            this.spendingPlanRepositoryMock.Object,
            this.validatorMock.Object,
            this.currentUserServiceMock.Object);
    }

    [Fact]
    public async Task CreateExpenseAsync_SpendingPlanNotFound_ReturnsNotFound()
    {
        this.spendingPlanRepositoryMock
            .Setup(r => r.GetByIdAsync(SpendingPlanId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SpendingPlan?)null);

        var result = await this.sut.CreateExpenseAsync(SpendingPlanId, BuildCreateRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task CreateExpenseAsync_Valid_PersistsWithCadence()
    {
        this.spendingPlanRepositoryMock
            .Setup(r => r.GetByIdAsync(SpendingPlanId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildSpendingPlan(UserId));

        var request = BuildCreateRequest() with { Cadence = SpendingPlanCadence.Weekly };
        var result = await this.sut.CreateExpenseAsync(SpendingPlanId, request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(SpendingPlanCadence.Weekly, result.ValueOrThrow.Cadence);
        this.expenseRepositoryMock.Verify(r => r.Add(It.Is<SpendingPlanExpense>(e => e.UserId == UserId && e.SpendingPlanId == SpendingPlanId)), Times.Once);
    }

    [Fact]
    public async Task CreateExpenseAsync_UnknownTag_ReturnsValidation()
    {
        this.spendingPlanRepositoryMock
            .Setup(r => r.GetByIdAsync(SpendingPlanId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildSpendingPlan(UserId));
        this.validatorMock
            .Setup(v => v.ValidateTagsAsync(It.IsAny<IReadOnlyCollection<int>>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Failure(DomainErrorType.Validation, "One or more of the specified tags do not exist"));

        var request = BuildCreateRequest() with { TagIds = new[] { 3 } };
        var result = await this.sut.CreateExpenseAsync(SpendingPlanId, request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task DeleteExpenseAsync_OtherUser_ReturnsForbidden()
    {
        this.expenseRepositoryMock
            .Setup(r => r.GetByIdAsync(1, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SpendingPlanExpense { Id = 1, UserId = OtherUserId, SpendingPlanId = SpendingPlanId });

        var result = await this.sut.DeleteExpenseAsync(SpendingPlanId, 1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Forbidden, result.ErrorType);
        this.expenseRepositoryMock.Verify(r => r.Remove(It.IsAny<SpendingPlanExpense>()), Times.Never);
    }

    private static CreateSpendingPlanExpenseRequest BuildCreateRequest()
    {
        return new CreateSpendingPlanExpenseRequest { Name = "Rent", Amount = 200000, Cadence = SpendingPlanCadence.Monthly };
    }

    private static SpendingPlan BuildSpendingPlan(int userId)
    {
        return new SpendingPlan { Id = SpendingPlanId, UserId = userId, Name = "SpendingPlan", Currency = "USD" };
    }
}