using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Constants;
using Cairnly.API.Data.Seeding;
using Cairnly.API.Extensions;
using Cairnly.API.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Cairnly.API.Data;

/// <summary>
/// Seeds the database with initial roles and an admin user.
/// </summary>
public sealed class DatabaseSeeder : IDatabaseSeeder
{
    private readonly DataContext context;

    private readonly UserManager<User> userManager;

    private readonly RoleManager<Role> roleManager;

    private readonly IConfiguration configuration;

    private readonly ILogger<DatabaseSeeder> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseSeeder"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="userManager">The Identity user manager.</param>
    /// <param name="roleManager">The Identity role manager.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="logger">The logger.</param>
    public DatabaseSeeder(
        DataContext context,
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        IConfiguration configuration,
        ILogger<DatabaseSeeder> logger)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        this.userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        this.roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        this.configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task SeedDatabaseAsync(bool seedData, bool clearCurrentData, bool applyMigrations, bool dropDatabase, CancellationToken cancellationToken = default)
    {
        if (dropDatabase)
        {
            await ResetSchemaAsync(this.context.Database.GetDbConnection(), cancellationToken);
        }

        if (applyMigrations)
        {
            await this.context.Database.MigrateAsync(cancellationToken);
        }

        if (clearCurrentData)
        {
            await this.ClearAllDataAsync(cancellationToken);
        }

        if (seedData)
        {
            await this.SeedRolesAsync(cancellationToken);
            await this.SeedAdminUserAsync(cancellationToken);
            await this.SeedSystemCategoriesAsync(cancellationToken);
            await this.SeedAdminSpendingPlanAsync(cancellationToken);

            await FixSequenceAsync(this.context, "AspNetRoles", "Id", cancellationToken);
            await FixSequenceAsync(this.context, "AspNetUsers", "Id", cancellationToken);

            await this.SeedLlmTestUserAsync(cancellationToken);
        }
    }

    private static async Task FixSequenceAsync(DbContext dbContext, string table, string idColumn, CancellationToken cancellationToken = default)
    {
        var sql = $@"
        SELECT setval(
            pg_get_serial_sequence('""{table}""', '{idColumn}'),
            COALESCE((SELECT MAX(""{idColumn}"") FROM ""{table}""), 0) + 1,
            false
        );";

        await dbContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private static async Task ResetSchemaAsync(DbConnection connection, CancellationToken ct)
    {
        await connection.OpenAsync(ct);

        try
        {
            await using var cmd1 = connection.CreateCommand();
            cmd1.CommandText = "DROP SCHEMA IF EXISTS public CASCADE;";
            await cmd1.ExecuteNonQueryAsync(ct);

            await using var cmd2 = connection.CreateCommand();
            cmd2.CommandText = "CREATE SCHEMA public;";
            await cmd2.ExecuteNonQueryAsync(ct);
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    private async Task ClearAllDataAsync(CancellationToken cancellationToken = default)
    {
        this.context.TransactionTags.Clear();
        this.context.Transactions.Clear();
        this.context.SpendingPlans.Clear();
        this.context.Tags.Clear();
        this.context.AccountBalanceSnapshots.Clear();
        this.context.Categories.Clear();
        this.context.Accounts.Clear();
        this.context.UserPreferences.Clear();
        this.context.RefreshTokens.Clear();
        this.context.LinkedAccounts.Clear();
        this.context.Users.Clear();
        this.context.Roles.Clear();

        await this.context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(CancellationToken cancellationToken = default)
    {
        if (await this.context.Roles.AnyAsync(cancellationToken))
        {
            return;
        }

        foreach (var roleName in new[] { UserRoleName.Admin, UserRoleName.User })
        {
            await this.roleManager.CreateAsync(new Role { Name = roleName });
        }
    }

    private async Task SeedSystemCategoriesAsync(CancellationToken cancellationToken = default)
    {
        if (await this.context.Categories.AnyAsync(c => c.IsSystem, cancellationToken))
        {
            return;
        }

        // System category groups (parent categories) and their child categories. The parent
        // group has no icon; children carry an emoji. Each child inherits its group's kind.
        var groups = SeedData.CategoryGroups;

        var userId = ApplicationSettings.SystemUserId;
        var categoryCount = 0;

        foreach (var (groupName, kind, children) in groups)
        {
            var parent = new Category
            {
                UserId = userId,
                Name = groupName,
                Kind = kind,
                IsSystem = true,
                Children = [.. children.Select(child => new Category
                {
                    UserId = userId,
                    Name = child.Name,
                    Icon = child.Icon,
                    Kind = kind,
                    IsSystem = true
                })]
            };

            this.context.Categories.Add(parent);
            categoryCount += 1 + children.Count;
        }

        this.logger.LogInformation(
            "Seeding {GroupCount} system category groups ({CategoryCount} categories)...",
            groups.Count,
            categoryCount);

        await this.context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAdminSpendingPlanAsync(CancellationToken cancellationToken = default)
    {
        if (await this.context.SpendingPlans.AnyAsync(cancellationToken))
        {
            return;
        }

        var userId = ApplicationSettings.SystemUserId;

        // Map leaf (child) category names to their seeded IDs so line items reference an assignable
        // category. Only children are included: groups are not assignable (leaf-only rule), and a
        // group can share a name with one of its children (e.g. "Shopping").
        var categoryIdByName = await this.context.Categories
            .Where(c => c.IsSystem && c.ParentId != null)
            .ToDictionaryAsync(c => c.Name, c => c.Id, cancellationToken);

        int CategoryId(string name) => categoryIdByName.TryGetValue(name, out var id)
            ? id
            : throw new InvalidOperationException($"Seed category '{name}' was not found.");

        // Essential / Non-Essential tags applied to expenses (owned by the admin user).
        var essentialTag = new Tag { UserId = userId, Name = "Essential", CreatedById = userId, UpdatedById = userId };
        var nonEssentialTag = new Tag { UserId = userId, Name = "Non-Essential", CreatedById = userId, UpdatedById = userId };
        this.context.Tags.AddRange(essentialTag, nonEssentialTag);
        await this.context.SaveChangesAsync(cancellationToken);

        // Income line items (all annual figures; the source has no per-income cadence).
        var incomes = SeedData.AdminIncomes;

        // Expense line items: amounts in integer minor units (cents). `Essential` drives the tag link.
        var expenses = SeedData.AdminExpenses;

        var spendingPlan = new SpendingPlan
        {
            UserId = userId,
            Name = "Main Spending Plan",
            Description = "Main spending plan",
            Currency = "USD",
            Incomes = [.. incomes
                .Select(income => new SpendingPlanIncome
                {
                    UserId = userId,
                    Name = income.Name,
                    Amount = income.Amount,
                    Cadence = SpendingPlanCadence.Annual,
                    CategoryId = CategoryId(income.Category)
                })],
            Expenses = [.. expenses
                .Select(expense => new SpendingPlanExpense
                {
                    UserId = userId,
                    Name = expense.Name,
                    Description = expense.Description,
                    Amount = expense.Amount,
                    Cadence = expense.Cadence,
                    CategoryId = CategoryId(expense.Category),
                    SpendingPlanExpenseTags =
                    [
                        new SpendingPlanExpenseTag { TagId = expense.Essential ? essentialTag.Id : nonEssentialTag.Id }
                    ]
                })]
        };

        this.logger.LogInformation(
            "Seeding admin spending plan with {IncomeCount} incomes and {ExpenseCount} expenses...",
            incomes.Count,
            expenses.Count);

        this.context.SpendingPlans.Add(spendingPlan);
        await this.context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedAdminUserAsync(CancellationToken cancellationToken = default)
    {
        if (await this.context.Users.AnyAsync(cancellationToken))
        {
            return;
        }

        var adminUser = new User
        {
            Id = ApplicationSettings.SystemUserId,
            UserName = "rob893",
            Email = "rwherber@gmail.com",
            EmailConfirmed = true,
            Created = DateTimeOffset.UtcNow,
            LastPasswordChange = DateTimeOffset.UtcNow,
            LastEmailChange = DateTimeOffset.UtcNow,
            LastUsernameChange = DateTimeOffset.UtcNow
        };

        this.logger.LogInformation("Seeding admin user...");
        var result = await this.userManager.CreateAsync(adminUser);

        if (!result.Succeeded)
        {
            this.logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", result.Errors));
            return;
        }

        await this.userManager.AddToRoleAsync(adminUser, UserRoleName.User);
        await this.userManager.AddToRoleAsync(adminUser, UserRoleName.Admin);
    }

    /// <summary>
    /// Dev-only: seeds the LLM test user (credentials from <c>LLMSecrets</c> in appsettings.Local.json)
    /// with multiple accounts, three years of transactions, balance snapshots, and spending plans.
    /// </summary>
    private async Task SeedLlmTestUserAsync(CancellationToken cancellationToken = default)
    {
        if (this.configuration.GetEnvironment() != EnvironmentNames.Development)
        {
            return;
        }

        var username = this.configuration[ConfigurationKeys.LlmTestUserName];
        var password = this.configuration[ConfigurationKeys.LlmTestPassword];

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            this.logger.LogInformation("LLMSecrets not configured; skipping LLM test user seed.");
            return;
        }

        var normalizedUsername = username.ToUpperInvariant();
        if (await this.context.Users.AnyAsync(u => u.NormalizedUserName == normalizedUsername, cancellationToken))
        {
            return;
        }

        var user = new User
        {
            UserName = username,
            Email = $"{username}@example.com",
            EmailConfirmed = true,
            Created = DateTimeOffset.UtcNow,
            LastPasswordChange = DateTimeOffset.UtcNow,
            LastEmailChange = DateTimeOffset.UtcNow,
            LastUsernameChange = DateTimeOffset.UtcNow
        };

        this.logger.LogInformation("Seeding LLM test user '{Username}'...", username);
        var result = await this.userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            this.logger.LogError("Failed to create LLM test user: {Errors}", string.Join(", ", result.Errors));
            return;
        }

        await this.userManager.AddToRoleAsync(user, UserRoleName.User);

        var categoryIdByName = await this.context.Categories
            .Where(c => c.IsSystem && c.ParentId != null)
            .ToDictionaryAsync(c => c.Name, c => c.Id, cancellationToken);

        await LlmTestUserSeeder.SeedAsync(this.context, user.Id, categoryIdByName, this.logger, cancellationToken);
    }
}