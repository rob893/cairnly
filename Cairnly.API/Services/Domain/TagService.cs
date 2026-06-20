using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Data.Repositories;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Requests.Tags;
using Cairnly.API.Services.Auth;
using Cairnly.API.Extensions;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Logging;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Service for tag management.
/// </summary>
public sealed class TagService : ITagService
{
    private readonly ILogger<TagService> logger;

    private readonly ITagRepository tagRepository;

    private readonly ICurrentUserService currentUserService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TagService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="tagRepository">The tag repository.</param>
    /// <param name="currentUserService">The current user service.</param>
    public TagService(
        ILogger<TagService> logger,
        ITagRepository tagRepository,
        ICurrentUserService currentUserService)
    {
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        this.currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    /// <inheritdoc />
    public async Task<CursorPaginatedList<TagDto, int>> GetTagsAsync(TagQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);

        queryParameters.RequestingUserId = this.currentUserService.UserId;
        queryParameters.RequestingUserIsAdmin = this.currentUserService.IsAdmin;

        var pagedList = await this.tagRepository.SearchAsync(queryParameters, track: false, cancellationToken);
        var mapped = pagedList
            .Select(TagDto.FromEntity)
            .ToList();

        return new CursorPaginatedList<TagDto, int>(mapped, pagedList.HasNextPage, pagedList.HasPreviousPage, pagedList.StartCursor, pagedList.EndCursor, pagedList.TotalCount);
    }

    /// <inheritdoc />
    public async Task<Result<TagDto>> GetTagByIdAsync(int id, CancellationToken cancellationToken)
    {
        var tag = await this.tagRepository.GetByIdAsync(id, track: false, cancellationToken);

        if (tag == null)
        {
            this.logger.LogWarning("Tag {TagId} not found", id);
            return Result<TagDto>.Failure(DomainErrorType.NotFound, "Tag not found");
        }

        if (!this.CanAccess(tag))
        {
            this.logger.LogWarning("User {UserId} attempted to access tag {TagId} owned by {OwnerId}", this.currentUserService.UserId, id, tag.UserId);
            return Result<TagDto>.Failure(DomainErrorType.Forbidden, "You can only access your own tags");
        }

        return Result<TagDto>.Success(TagDto.FromEntity(tag));
    }

    /// <inheritdoc />
    public async Task<Result<TagDto>> CreateTagAsync(CreateTagRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = this.currentUserService.UserId;
        var existing = await this.tagRepository.GetByNameAsync(userId, request.Name, cancellationToken);

        if (existing != null)
        {
            return Result<TagDto>.Failure(DomainErrorType.Conflict, "A tag with this name already exists");
        }

        var tag = new Tag
        {
            UserId = userId,
            Name = request.Name,
            Metadata = request.Metadata ?? [],
            CreatedById = this.currentUserService.UserId,
            UpdatedById = this.currentUserService.UserId
        };

        this.tagRepository.Add(tag);
        await this.tagRepository.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Created tag {TagId} for user {UserId}", tag.Id, tag.UserId);

        return Result<TagDto>.Success(TagDto.FromEntity(tag));
    }

    /// <inheritdoc />
    public async Task<Result<TagDto>> UpdateTagAsync(int id, UpdateTagRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tag = await this.tagRepository.GetByIdAsync(id, track: true, cancellationToken);

        if (tag == null)
        {
            this.logger.LogWarning("Tag {TagId} not found for update", id);
            return Result<TagDto>.Failure(DomainErrorType.NotFound, "Tag not found");
        }

        if (!this.CanAccess(tag))
        {
            this.logger.LogWarning("User {UserId} attempted to update tag {TagId} owned by {OwnerId}", this.currentUserService.UserId, id, tag.UserId);
            return Result<TagDto>.Failure(DomainErrorType.Forbidden, "You can only update your own tags");
        }

        var duplicate = await this.tagRepository.GetByNameAsync(tag.UserId, request.Name, cancellationToken);

        if (duplicate != null && duplicate.Id != tag.Id)
        {
            return Result<TagDto>.Failure(DomainErrorType.Conflict, "A tag with this name already exists");
        }

        tag.Name = request.Name;
        tag.Metadata = request.Metadata ?? [];
        tag.UpdatedById = this.currentUserService.UserId;

        await this.tagRepository.SaveChangesAsync(cancellationToken);

        return Result<TagDto>.Success(TagDto.FromEntity(tag));
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteTagAsync(int id, CancellationToken cancellationToken)
    {
        var tag = await this.tagRepository.GetByIdAsync(id, track: true, cancellationToken);

        if (tag == null)
        {
            this.logger.LogWarning("Tag {TagId} not found for deletion", id);
            return Result<bool>.Failure(DomainErrorType.NotFound, "Tag not found");
        }

        if (!this.CanAccess(tag))
        {
            this.logger.LogWarning("User {UserId} attempted to delete tag {TagId} owned by {OwnerId}", this.currentUserService.UserId, id, tag.UserId);
            return Result<bool>.Failure(DomainErrorType.Forbidden, "You can only delete your own tags");
        }

        this.tagRepository.Remove(tag);
        await this.tagRepository.SaveChangesAsync(cancellationToken);

        this.logger.LogInformation("Deleted tag {TagId} for user {UserId}", id, this.currentUserService.UserId);

        return Result<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<Result<TagDto>> PatchTagAsync(int id, JsonPatchDocument<UpdateTagRequest> patchDocument, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(patchDocument);

        if (patchDocument.Operations.Count == 0)
        {
            return Result<TagDto>.Failure(DomainErrorType.Validation, "A JSON patch document with at least 1 operation is required.");
        }

        var tag = await this.tagRepository.GetByIdAsync(id, track: false, cancellationToken);

        if (tag == null)
        {
            return Result<TagDto>.Failure(DomainErrorType.NotFound, "Tag not found");
        }

        if (!this.CanAccess(tag))
        {
            this.logger.LogWarning("User {UserId} attempted to patch tag {TagId} owned by {OwnerId}", this.currentUserService.UserId, id, tag.UserId);
            return Result<TagDto>.Failure(DomainErrorType.Forbidden, "You can only update your own tags");
        }

        var request = UpdateTagRequest.FromEntity(tag);

        if (!patchDocument.TryApply(request, out var patchError))
        {
            return Result<TagDto>.Failure(DomainErrorType.Validation, $"Invalid JSON patch document: {patchError}");
        }

        if (!request.TryValidate(out var validationError))
        {
            return Result<TagDto>.Failure(DomainErrorType.Validation, validationError);
        }

        return await this.UpdateTagAsync(id, request, cancellationToken);
    }

    private bool CanAccess(Tag tag)
    {
        return tag.UserId == this.currentUserService.UserId || this.currentUserService.IsAdmin;
    }
}
