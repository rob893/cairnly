using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Data.Repositories;
using Cairnly.API.Models.Entities;
using Cairnly.API.Services.Auth;
using Cairnly.API.Services.Domain;
using Moq;

namespace Cairnly.API.Tests.Services;

/// <summary>
/// Tests for <see cref="CategoryTagValidator"/>, focusing on category assignment rules.
/// </summary>
public sealed class CategoryTagValidatorTests
{
    private const int UserId = 42;

    private readonly Mock<ICategoryRepository> categoryRepositoryMock;
    private readonly Mock<ITagRepository> tagRepositoryMock;
    private readonly Mock<ICurrentUserService> currentUserServiceMock;
    private readonly CategoryTagValidator sut;

    public CategoryTagValidatorTests()
    {
        this.categoryRepositoryMock = new Mock<ICategoryRepository>();
        this.tagRepositoryMock = new Mock<ITagRepository>();
        this.currentUserServiceMock = new Mock<ICurrentUserService>();
        this.currentUserServiceMock.Setup(s => s.UserId).Returns(UserId);
        this.currentUserServiceMock.Setup(s => s.IsAdmin).Returns(false);

        this.sut = new CategoryTagValidator(
            this.categoryRepositoryMock.Object,
            this.tagRepositoryMock.Object,
            this.currentUserServiceMock.Object);
    }

    [Fact]
    public async Task ValidateCategoryAsync_Null_ReturnsValidation()
    {
        var result = await this.sut.ValidateCategoryAsync(null, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task ValidateCategoryAsync_Inaccessible_ReturnsValidation()
    {
        this.categoryRepositoryMock
            .Setup(r => r.GetByIdAsync(5, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var result = await this.sut.ValidateCategoryAsync(5, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task ValidateCategoryAsync_GroupWithChildren_ReturnsValidation()
    {
        this.categoryRepositoryMock
            .Setup(r => r.GetByIdAsync(5, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category { Id = 5, UserId = UserId, Name = "Housing", Kind = CategoryKind.Expense });
        this.categoryRepositoryMock
            .Setup(r => r.HasChildrenAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await this.sut.ValidateCategoryAsync(5, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Validation, result.ErrorType);
        Assert.Contains("category group", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateCategoryAsync_Leaf_ReturnsSuccess()
    {
        this.categoryRepositoryMock
            .Setup(r => r.GetByIdAsync(5, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Category { Id = 5, UserId = UserId, ParentId = 1, Name = "Mortgage", Kind = CategoryKind.Expense });
        this.categoryRepositoryMock
            .Setup(r => r.HasChildrenAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await this.sut.ValidateCategoryAsync(5, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }
}