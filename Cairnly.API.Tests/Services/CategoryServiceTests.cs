using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Data.Repositories;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.Requests.Categories;
using Cairnly.API.Services.Auth;
using Cairnly.API.Services.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Cairnly.API.Tests.Services;

/// <summary>
/// Tests for <see cref="CategoryService"/>.
/// </summary>
public sealed class CategoryServiceTests
{
    private const int UserId = 42;
    private const int OtherUserId = 99;
    private const int SystemUserId = 1;

    private readonly Mock<ICategoryRepository> categoryRepositoryMock;
    private readonly Mock<ICurrentUserService> currentUserServiceMock;
    private readonly CategoryService sut;

    public CategoryServiceTests()
    {
        this.categoryRepositoryMock = new Mock<ICategoryRepository>();
        this.currentUserServiceMock = new Mock<ICurrentUserService>();
        this.currentUserServiceMock.Setup(s => s.UserId).Returns(UserId);
        this.currentUserServiceMock.Setup(s => s.IsAdmin).Returns(false);

        this.sut = new CategoryService(
            NullLogger<CategoryService>.Instance,
            this.categoryRepositoryMock.Object,
            this.currentUserServiceMock.Object);
    }

    [Fact]
    public async Task GetCategoryByIdAsync_SystemCategory_IsAccessibleToNonOwner()
    {
        this.categoryRepositoryMock
            .Setup(r => r.GetByIdAsync(1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildCategory(1, SystemUserId, isSystem: true));

        var result = await this.sut.GetCategoryByIdAsync(1, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.ValueOrThrow.IsSystem);
    }

    [Fact]
    public async Task GetCategoryByIdAsync_OtherUserNonSystem_ReturnsForbidden()
    {
        this.categoryRepositoryMock
            .Setup(r => r.GetByIdAsync(1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildCategory(1, OtherUserId, isSystem: false));

        var result = await this.sut.GetCategoryByIdAsync(1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task UpdateCategoryAsync_SystemCategoryByNonAdmin_ReturnsForbidden()
    {
        this.categoryRepositoryMock
            .Setup(r => r.GetByIdAsync(1, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildCategory(1, SystemUserId, isSystem: true));

        var request = new UpdateCategoryRequest { Name = "Renamed", Kind = CategoryKind.Expense };
        var result = await this.sut.UpdateCategoryAsync(1, request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task CreateCategoryAsync_InvalidParent_ReturnsValidation()
    {
        this.categoryRepositoryMock
            .Setup(r => r.GetByIdAsync(7, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Category?)null);

        var request = new CreateCategoryRequest { Name = "Sub", Kind = CategoryKind.Expense, ParentId = 7 };
        var result = await this.sut.CreateCategoryAsync(request, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task CreateCategoryAsync_Valid_PersistsAsUserOwnedNonSystem()
    {
        var request = new CreateCategoryRequest { Name = "Coffee", Kind = CategoryKind.Expense };

        var result = await this.sut.CreateCategoryAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.ValueOrThrow.IsSystem);
        Assert.Equal(UserId, result.ValueOrThrow.UserId);
        this.categoryRepositoryMock.Verify(r => r.Add(It.Is<Category>(c => !c.IsSystem && c.UserId == UserId)), Times.Once);
    }

    private static Category BuildCategory(int id, int userId, bool isSystem)
    {
        return new Category
        {
            Id = id,
            UserId = userId,
            Name = "Category",
            Kind = CategoryKind.Expense,
            IsSystem = isSystem
        };
    }
}
