using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Core;
using Cairnly.API.Data.Repositories;
using Cairnly.API.Services.Auth;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Default implementation of <see cref="ICategoryTagValidator"/>.
/// </summary>
public sealed class CategoryTagValidator : ICategoryTagValidator
{
    private readonly ICategoryRepository categoryRepository;

    private readonly ITagRepository tagRepository;

    private readonly ICurrentUserService currentUserService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryTagValidator"/> class.
    /// </summary>
    /// <param name="categoryRepository">The category repository.</param>
    /// <param name="tagRepository">The tag repository.</param>
    /// <param name="currentUserService">The current user service.</param>
    public CategoryTagValidator(
        ICategoryRepository categoryRepository,
        ITagRepository tagRepository,
        ICurrentUserService currentUserService)
    {
        this.categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        this.tagRepository = tagRepository ?? throw new ArgumentNullException(nameof(tagRepository));
        this.currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    /// <inheritdoc />
    public async Task<Result<bool>> ValidateCategoryAsync(int? categoryId, CancellationToken cancellationToken)
    {
        if (categoryId == null)
        {
            return Result<bool>.Failure(DomainErrorType.Validation, "A category is required");
        }

        var category = await this.categoryRepository.GetByIdAsync(categoryId.Value, track: false, cancellationToken);

        var accessible = category != null
            && (category.UserId == this.currentUserService.UserId || category.IsSystem || this.currentUserService.IsAdmin);

        if (!accessible)
        {
            return Result<bool>.Failure(DomainErrorType.Validation, "The specified category does not exist");
        }

        // Parent groups are organizational only; line items and transactions must be assigned to a
        // leaf category (one with no children) so group rollups stay unambiguous.
        var hasChildren = await this.categoryRepository.HasChildrenAsync(categoryId.Value, cancellationToken);
        if (hasChildren)
        {
            return Result<bool>.Failure(
                DomainErrorType.Validation,
                $"'{category!.Name}' is a category group; choose one of its categories instead");
        }

        return Result<bool>.Success(true);
    }

    /// <inheritdoc />
    public async Task<Result<bool>> ValidateTagsAsync(IReadOnlyCollection<int> tagIds, int ownerUserId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(tagIds);

        if (tagIds.Count == 0)
        {
            return Result<bool>.Success(true);
        }

        var tags = await this.tagRepository.SearchAsync(
            t => tagIds.Contains(t.Id) && t.UserId == ownerUserId,
            track: false,
            cancellationToken);

        if (tags.Count != tagIds.Count)
        {
            return Result<bool>.Failure(DomainErrorType.Validation, "One or more of the specified tags do not exist");
        }

        return Result<bool>.Success(true);
    }
}