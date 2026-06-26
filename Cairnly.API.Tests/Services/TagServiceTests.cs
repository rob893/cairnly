using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Data.Repositories;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.Requests.Tags;
using Cairnly.API.Services.Auth;
using Cairnly.API.Services.Domain;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Cairnly.API.Tests.Services;

/// <summary>
/// Tests for <see cref="TagService"/>.
/// </summary>
public sealed class TagServiceTests
{
    private const int UserId = 42;
    private const int OtherUserId = 99;

    private readonly Mock<ITagRepository> tagRepositoryMock;
    private readonly Mock<ICurrentUserService> currentUserServiceMock;
    private readonly TagService sut;

    public TagServiceTests()
    {
        this.tagRepositoryMock = new Mock<ITagRepository>();
        this.currentUserServiceMock = new Mock<ICurrentUserService>();
        this.currentUserServiceMock.Setup(s => s.UserId).Returns(UserId);
        this.currentUserServiceMock.Setup(s => s.IsAdmin).Returns(false);

        this.sut = new TagService(
            NullLogger<TagService>.Instance,
            this.tagRepositoryMock.Object,
            this.currentUserServiceMock.Object);
    }

    [Fact]
    public async Task GetTagByIdAsync_OtherUser_ReturnsForbidden()
    {
        this.tagRepositoryMock
            .Setup(r => r.GetByIdAsync(1, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildTag(1, OtherUserId));

        var result = await this.sut.GetTagByIdAsync(1, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task CreateTagAsync_DuplicateName_ReturnsConflict()
    {
        this.tagRepositoryMock
            .Setup(r => r.GetByNameAsync(UserId, "Vacation", It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildTag(5, UserId));

        var result = await this.sut.CreateTagAsync(new CreateTagRequest { Name = "Vacation" }, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(DomainErrorType.Conflict, result.ErrorType);
        this.tagRepositoryMock.Verify(r => r.Add(It.IsAny<Tag>()), Times.Never);
    }

    [Fact]
    public async Task CreateTagAsync_Unique_Persists()
    {
        this.tagRepositoryMock
            .Setup(r => r.GetByNameAsync(UserId, "Vacation", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tag?)null);

        var result = await this.sut.CreateTagAsync(new CreateTagRequest { Name = "Vacation" }, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Vacation", result.ValueOrThrow.Name);
        this.tagRepositoryMock.Verify(r => r.Add(It.Is<Tag>(t => t.UserId == UserId)), Times.Once);
        this.tagRepositoryMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Tag BuildTag(int id, int userId)
    {
        return new Tag { Id = id, UserId = userId, Name = "Tag" };
    }
}