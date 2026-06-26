using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Constants;
using Cairnly.API.Extensions;
using Cairnly.API.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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

    private readonly ILogger<DatabaseSeeder> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseSeeder"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="userManager">The Identity user manager.</param>
    /// <param name="roleManager">The Identity role manager.</param>
    /// <param name="logger">The logger.</param>
    public DatabaseSeeder(
        DataContext context,
        UserManager<User> userManager,
        RoleManager<Role> roleManager,
        ILogger<DatabaseSeeder> logger)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));
        this.userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        this.roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
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
        var groups = new (string Group, CategoryKind Kind, (string Name, string Icon)[] Children)[]
        {
            ("Income", CategoryKind.Income,
            [
                ("Paychecks", "💵"),
                ("Interest", "💸"),
                ("Business Income", "💰"),
                ("Other Income", "💰"),
                ("Dividends & Capital Gains", "📈")
            ]),
            ("Gifts & Donations", CategoryKind.Expense,
            [
                ("Charity", "🎗"),
                ("Gifts", "🎁")
            ]),
            ("Auto & Transport", CategoryKind.Expense,
            [
                ("Auto Payment", "🚗"),
                ("Public Transit", "🚃"),
                ("Gas", "⛽️"),
                ("Auto Maintenance", "🔧"),
                ("Parking & Tolls", "🏢"),
                ("Taxi & Ride Shares", "🚕")
            ]),
            ("Housing", CategoryKind.Expense,
            [
                ("Mortgage", "🏠"),
                ("Rent", "🏠"),
                ("Home Improvement", "🔨")
            ]),
            ("Bills & Utilities", CategoryKind.Expense,
            [
                ("Garbage", "🗑"),
                ("Water", "💧"),
                ("Gas & Electric", "⚡️"),
                ("Internet & Cable", "🌐"),
                ("Phone", "📱")
            ]),
            ("Food & Dining", CategoryKind.Expense,
            [
                ("Groceries", "🍏"),
                ("Restaurants & Bars", "🍽"),
                ("Coffee Shops", "☕️")
            ]),
            ("Travel & Lifestyle", CategoryKind.Expense,
            [
                ("Travel & Vacation", "🏝"),
                ("Entertainment & Recreation", "🎥"),
                ("Personal", "👑"),
                ("Pets", "🐶"),
                ("Fun Money", "😜")
            ]),
            ("Shopping", CategoryKind.Expense,
            [
                ("Shopping", "🛍"),
                ("Clothing", "👕"),
                ("Furniture & Housewares", "🪑"),
                ("Electronics", "🖥")
            ]),
            ("Children", CategoryKind.Expense,
            [
                ("Child Care", "👶"),
                ("Child Activities", "⚽️")
            ]),
            ("Education", CategoryKind.Expense,
            [
                ("Student Loans", "🎓"),
                ("Education", "🏫")
            ]),
            ("Health & Wellness", CategoryKind.Expense,
            [
                ("Medical", "💊"),
                ("Dentist", "🦷"),
                ("Fitness", "💪")
            ]),
            ("Financial", CategoryKind.Expense,
            [
                ("Loan Repayment", "💰"),
                ("Financial & Legal Services", "🗄"),
                ("Financial Fees", "🏦"),
                ("Cash & ATM", "🏧"),
                ("Insurance", "☂️"),
                ("Taxes", "🏛️")
            ]),
            ("Other", CategoryKind.Expense,
            [
                ("Uncategorized", "❓"),
                ("Check", "💸"),
                ("Miscellaneous", "💲")
            ]),
            ("Business", CategoryKind.Expense,
            [
                ("Advertising & Promotion", "📣"),
                ("Business Utilities & Communication", "📞"),
                ("Employee Wages & Contract Labor", "💵"),
                ("Business Travel & Meals", "🍴"),
                ("Business Auto Expenses", "🚖"),
                ("Business Insurance", "📁"),
                ("Office Supplies & Expenses", "📎"),
                ("Office Rent", "🏢"),
                ("Postage & Shipping", "📦")
            ]),
            ("Transfers", CategoryKind.Transfer,
            [
                ("Transfer", "🔁"),
                ("Credit Card Payment", "💳"),
                ("Balance Adjustments", "⚖️"),
                ("Buy", "➡️"),
                ("Sell", "⬅️")
            ])
        };

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
            categoryCount += 1 + children.Length;
        }

        this.logger.LogInformation(
            "Seeding {GroupCount} system category groups ({CategoryCount} categories)...",
            groups.Length,
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

        // Income line items (all annual figures; the source has no per-income cadence).
        var incomes = new (string Name, IncomeType Type, long Amount)[]
        {
            ("Interest and Dividends", IncomeType.W2, 300_000),
            ("Microsoft W2", IncomeType.W2, 19_000_000),
            ("Microsoft Perks+", IncomeType.W2, 150_000),
            ("Microsoft Bonus", IncomeType.W2, 3_800_000),
            ("Microsoft Stock", IncomeType.W2, 6_000_000)
        };

        // Expense line items: amounts in integer minor units (cents).
        var expenses = new (string Name, string? Description, long Amount, SpendingPlanCadence Cadence)[]
        {
            ("Taxable Brokerage", "Robinhood Account", 50_000, SpendingPlanCadence.Weekly),
            ("Mortgage, HOA, Utilities", "3026.83 for Mortgage/tax/insurance\n150 HOA\n~500 for utilities (power, gas, water, trash)", 400_000, SpendingPlanCadence.Monthly),
            ("Netflix", null, 500, SpendingPlanCadence.Monthly),
            ("Disney+", null, 500, SpendingPlanCadence.Monthly),
            ("Haircut", null, 5_000, SpendingPlanCadence.Monthly),
            ("Hulu", null, 500, SpendingPlanCadence.Monthly),
            ("Internet", null, 7_000, SpendingPlanCadence.Monthly),
            ("Phone", null, 9_500, SpendingPlanCadence.Monthly),
            ("WoW", null, 1_500, SpendingPlanCadence.Monthly),
            ("Amazon Prime", null, 14_000, SpendingPlanCadence.Annual),
            ("Car Registration", "GA is super cheap lol", 2_000, SpendingPlanCadence.Annual),
            ("Domain Names", "rwherber.com and knowones.com and derpcode and tidygal\nManaged through godaddy", 8_000, SpendingPlanCadence.Annual),
            ("Web Server Hosting", "Digital ocean VM", 2_900, SpendingPlanCadence.Monthly),
            ("YouTube Premium", null, 1_000, SpendingPlanCadence.Monthly),
            ("Food (Groceries/Going Out)", null, 40_000, SpendingPlanCadence.Monthly),
            ("Gas", null, 7_500, SpendingPlanCadence.Monthly),
            ("Car Insurance", null, 15_300, SpendingPlanCadence.Monthly),
            ("401k", null, 2_300_000, SpendingPlanCadence.Annual),
            ("Federal Tax", "From 2024 tax return", 7_520_600, SpendingPlanCadence.Annual),
            ("GA State Tax", "From 2024 tax return", 1_200_000, SpendingPlanCadence.Annual),
            ("Amazon Fresh Membership", "Fresh Membership for free deliveries", 1_000, SpendingPlanCadence.Monthly),
            ("Pet Insurance", null, 5_600, SpendingPlanCadence.Monthly),
            ("HBO Max", null, 500, SpendingPlanCadence.Monthly),
            ("Pest Control", null, 35_200, SpendingPlanCadence.Annual),
            ("Misc Spending", null, 15_000, SpendingPlanCadence.Weekly),
            ("Mortgage Additional Principal Payments", null, 100_000, SpendingPlanCadence.Monthly),
            ("House cleaning", null, 25_000, SpendingPlanCadence.Monthly),
            ("Termite Bond", null, 35_000, SpendingPlanCadence.Annual),
            ("OpenAI Sub", null, 2_000, SpendingPlanCadence.Monthly),
            ("HSA", null, 440_000, SpendingPlanCadence.Annual),
            ("Umbrella Insurance", null, 33_000, SpendingPlanCadence.Annual)
        };

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
                    Type = income.Type,
                    Amount = income.Amount,
                    Cadence = SpendingPlanCadence.Annual
                })],
            Expenses = [.. expenses
                .Select(expense => new SpendingPlanExpense
                {
                    UserId = userId,
                    Name = expense.Name,
                    Description = expense.Description,
                    Amount = expense.Amount,
                    Cadence = expense.Cadence
                })]
        };

        this.logger.LogInformation(
            "Seeding admin spending plan with {IncomeCount} incomes and {ExpenseCount} expenses...",
            incomes.Length,
            expenses.Length);

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
}
