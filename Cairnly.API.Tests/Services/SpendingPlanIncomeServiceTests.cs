using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Data.Repositories;
using Cairnly.API.Models;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.Requests.SpendingPlanIncomes;
using Cairnly.API.Services.Auth;
using Cairnly.API.Services.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Cairnly.API.Tests.Services;

/// <summary>
/// Tests for <see cref="SpendingPlanIncomeService"/>.
/// </summary>
public sealed class SpendingPlanIncomeServiceTests
{
    private const int UserId = 42;
    private const int OtherUserId = 99;
    private const int SpendingPlanId = 7;

    private readonly Mock<ISpendingPlanIncomeRepository> incomeRepositoryMock;
    private readonly Mock<ISpendingPlanRepository> spendingPlanRepositoryMock;
    private readonly Mock<ICategoryTagValidator> validatorMock;
    private readonly Mock<ICurrentUserService> currentUserServiceMock;
    private readonly SpendingPlanIncomeService sut;

    public SpendingPlanIncomeServiceTests()
    {
        this.incomeRepositoryMock = new Mock<ISpendingPlanIncomeRepository>();
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

        this.sut = new SpendingPlanIncomeService(
            NullLogger<SpendingPlanIncomeService>.Instance,
            this.incomeRepositoryMock.Object,
            this.spendingPlanRepositoryMock.Object,
            this.validatorMock.Object,
            this.currentUserServiceMock.Object);
    }

    [Fact]
    public async Task CreateIncomeAsync_SpendingPlanNotFound_ReturnsNotFound()
    {
        this.spendingPlanRepositoryMock
            .Setup(r => r.GetByIdAsync(SpendingPlanId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SpendingPlan?)null);

        var result = await this.sut.CreateIncomeAsync(SpendingPlanId, BuildCreateRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task CreateIncomeAsync_SpendingPlanOwnedByOther_ReturnsForbidden()
    {
        this.spendingPlanRepositoryMock
            .Setup(r => r.GetByIdAsync(SpendingPlanId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildSpendingPlan(OtherUserId));

        var result = await this.sut.CreateIncomeAsync(SpendingPlanId, BuildCreateRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task CreateIncomeAsync_InvalidCategory_ReturnsValidation()
    {
        this.spendingPlanRepositoryMock
            .Setup(r => r.GetByIdAsync(SpendingPlanId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildSpendingPlan(UserId));
        this.validatorMock
            .Setup(v => v.ValidateCategoryAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Failure(DomainErrorType.Validation, "The specified category does not exist"));

        var request = BuildCreateRequest() with { CategoryId = 5 };
        var result = await this.sut.CreateIncomeAsync(SpendingPlanId, request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task CreateIncomeAsync_Valid_PersistsAndMapsTags()
    {
        this.spendingPlanRepositoryMock
            .Setup(r => r.GetByIdAsync(SpendingPlanId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildSpendingPlan(UserId));

        var request = BuildCreateRequest() with { TagIds = new[] { 3 } };
        var result = await this.sut.CreateIncomeAsync(SpendingPlanId, request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(SpendingPlanId, result.ValueOrThrow.SpendingPlanId);
        Assert.Equal(new[] { 3 }, result.ValueOrThrow.TagIds);
        this.incomeRepositoryMock.Verify(r => r.Add(It.Is<SpendingPlanIncome>(i => i.UserId == UserId && i.SpendingPlanId == SpendingPlanId)), Times.Once);
    }

    [Fact]
    public async Task GetIncomeByIdAsync_WrongSpendingPlan_ReturnsNotFound()
    {
        this.incomeRepositoryMock
            .Setup(r => r.GetByIdAsync(1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SpendingPlanIncome { Id = 1, UserId = UserId, SpendingPlanId = 999 });

        var result = await this.sut.GetIncomeByIdAsync(SpendingPlanId, 1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task DeleteIncomeAsync_OtherUser_ReturnsForbidden()
    {
        this.incomeRepositoryMock
            .Setup(r => r.GetByIdAsync(1, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SpendingPlanIncome { Id = 1, UserId = OtherUserId, SpendingPlanId = SpendingPlanId });

        var result = await this.sut.DeleteIncomeAsync(SpendingPlanId, 1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Forbidden, result.ErrorType);
        this.incomeRepositoryMock.Verify(r => r.Remove(It.IsAny<SpendingPlanIncome>()), Times.Never);
    }

    private static CreateSpendingPlanIncomeRequest BuildCreateRequest()
    {
        return new CreateSpendingPlanIncomeRequest { Name = "Salary", Type = IncomeType.W2, Amount = 100000, Cadence = SpendingPlanCadence.Annual };
    }

    private static SpendingPlan BuildSpendingPlan(int userId)
    {
        return new SpendingPlan { Id = SpendingPlanId, UserId = userId, Name = "SpendingPlan", Currency = "USD" };
    }
}
