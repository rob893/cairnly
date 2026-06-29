using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Data.Repositories;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Requests.SpendingPlanIncomes;
using Cairnly.API.Services.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Cairnly.API.Tests.Services;

/// <summary>
/// Tests for <see cref="SpendingPlanIncomeService"/>.
/// </summary>
public sealed class SpendingPlanIncomeServiceTests : SpendingPlanLineItemServiceTestBase<SpendingPlanIncome, SpendingPlanIncomeDto, CreateSpendingPlanIncomeRequest, SpendingPlanIncomeQueryParameters, ISpendingPlanIncomeRepository>
{
    private readonly SpendingPlanIncomeService sut;

    public SpendingPlanIncomeServiceTests()
    {
        this.sut = new SpendingPlanIncomeService(
            NullLogger<SpendingPlanIncomeService>.Instance,
            this.LineItemRepositoryMock.Object,
            this.SpendingPlanRepositoryMock.Object,
            this.ValidatorMock.Object,
            this.CurrentUserServiceMock.Object);
    }

    [Fact]
    public async Task CreateIncomeAsync_SpendingPlanOwnedByOther_ReturnsForbidden()
    {
        this.SpendingPlanRepositoryMock
            .Setup(r => r.GetByIdAsync(SpendingPlanId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildSpendingPlan(OtherUserId));

        var result = await this.sut.CreateIncomeAsync(SpendingPlanId, this.BuildCreateRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task CreateIncomeAsync_InvalidCategory_ReturnsValidation()
    {
        this.SpendingPlanRepositoryMock
            .Setup(r => r.GetByIdAsync(SpendingPlanId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildSpendingPlan(UserId));
        this.ValidatorMock
            .Setup(v => v.ValidateCategoryAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Failure(DomainErrorType.Validation, "The specified category does not exist"));

        var request = this.BuildCreateRequest() with { CategoryId = 5 };
        var result = await this.sut.CreateIncomeAsync(SpendingPlanId, request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task CreateIncomeAsync_Valid_PersistsAndMapsTags()
    {
        this.SpendingPlanRepositoryMock
            .Setup(r => r.GetByIdAsync(SpendingPlanId, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildSpendingPlan(UserId));

        var request = this.BuildCreateRequest() with { TagIds = new[] { 3 } };
        var result = await this.sut.CreateIncomeAsync(SpendingPlanId, request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(SpendingPlanId, result.ValueOrThrow.SpendingPlanId);
        Assert.Equal(new[] { 3 }, result.ValueOrThrow.TagIds);
        this.LineItemRepositoryMock.Verify(r => r.Add(It.Is<SpendingPlanIncome>(i => i.UserId == UserId && i.SpendingPlanId == SpendingPlanId)), Times.Once);
    }

    [Fact]
    public async Task GetIncomeByIdAsync_WrongSpendingPlan_ReturnsNotFound()
    {
        this.LineItemRepositoryMock
            .Setup(r => r.GetByIdAsync(1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SpendingPlanIncome { Id = 1, UserId = UserId, SpendingPlanId = 999 });

        var result = await this.sut.GetIncomeByIdAsync(SpendingPlanId, 1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.NotFound, result.ErrorType);
    }

    /// <inheritdoc />
    protected override Task<Result<SpendingPlanIncomeDto>> CreateAsync(int spendingPlanId, CreateSpendingPlanIncomeRequest request, CancellationToken cancellationToken)
    {
        return this.sut.CreateIncomeAsync(spendingPlanId, request, cancellationToken);
    }

    /// <inheritdoc />
    protected override Task<Result<bool>> DeleteAsync(int spendingPlanId, int lineItemId, CancellationToken cancellationToken)
    {
        return this.sut.DeleteIncomeAsync(spendingPlanId, lineItemId, cancellationToken);
    }

    /// <inheritdoc />
    protected override CreateSpendingPlanIncomeRequest BuildCreateRequest()
    {
        return new CreateSpendingPlanIncomeRequest { Name = "Salary", Amount = 100000, Cadence = SpendingPlanCadence.Annual, CategoryId = 1 };
    }

    /// <inheritdoc />
    protected override SpendingPlanIncome BuildLineItem(int id, int userId, int spendingPlanId)
    {
        return new SpendingPlanIncome { Id = id, UserId = userId, SpendingPlanId = spendingPlanId };
    }
}