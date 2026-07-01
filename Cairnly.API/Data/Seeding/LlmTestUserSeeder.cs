using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Models.Entities;
using Microsoft.Extensions.Logging;

namespace Cairnly.API.Data.Seeding;

/// <summary>
/// Generates a rich, deterministic dataset for the dev-only LLM test user: multiple accounts, three
/// years of monthly transactions, matching balance snapshots (so net-worth history looks regularly
/// tracked), and a couple of spending plans. Generation logic lives here, separate from the seeder.
/// </summary>
public static class LlmTestUserSeeder
{
    private const string Currency = "USD";

    /// <summary>Number of months of history to generate.</summary>
    private const int Months = 36;

    private sealed record MonthlyExpense(string Merchant, string Category, long Min, long Max, int Count);

    // Recurring monthly checking-account expenses (amount ranges in cents, drawn deterministically).
    private static readonly MonthlyExpense[] expenses =
    [
        new("Bank Mortgage", "Mortgage", 180_000, 180_000, 1),
        new("Whole Foods", "Groceries", 38_000, 62_000, 2),
        new("Shell", "Gas", 18_000, 30_000, 1),
        new("Georgia Power", "Gas & Electric", 9_000, 16_000, 1),
        new("Comcast", "Internet & Cable", 7_000, 7_000, 1),
        new("Verizon", "Phone", 9_500, 9_500, 1),
        new("Olive Garden", "Restaurants & Bars", 4_000, 12_000, 2),
        new("Starbucks", "Coffee Shops", 600, 1_800, 3),
        new("Amazon", "Shopping", 3_000, 20_000, 2)
    ];

    /// <summary>
    /// Seeds the LLM test user's accounts, transactions, balance snapshots, and spending plans.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="userId">The seeded user's ID.</param>
    /// <param name="categoryIdByName">Leaf-category name to ID lookup.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public static async Task SeedAsync(DataContext context, int userId, IReadOnlyDictionary<string, int> categoryIdByName, ILogger logger, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(categoryIdByName);
        ArgumentNullException.ThrowIfNull(logger);

        int Cat(string name) => categoryIdByName.TryGetValue(name, out var id)
            ? id
            : throw new InvalidOperationException($"Seed category '{name}' was not found.");

        // Deterministic, dev-only seed data; not security-sensitive.
#pragma warning disable CA5394
        var rng = new Random(8675309);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var start = today.AddMonths(-Months);

        var checking = NewAccount(userId, "Everyday Checking", AccountType.Checking, AccountClass.Asset, 500_000, start);
        var savings = NewAccount(userId, "High-Yield Savings", AccountType.Savings, AccountClass.Asset, 1_500_000, start);
        var brokerage = NewAccount(userId, "Brokerage", AccountType.Brokerage, AccountClass.Asset, 2_500_000, start);
        var card = NewAccount(userId, "Rewards Card", AccountType.CreditCard, AccountClass.Liability, 0, start);
        var mortgage = NewAccount(userId, "Home Mortgage", AccountType.Mortgage, AccountClass.Liability, -28_000_000, start);
        context.Accounts.AddRange(checking, savings, brokerage, card, mortgage);
        await context.SaveChangesAsync(cancellationToken);

        var transactions = new List<Transaction>();
        var snapshots = new List<AccountBalanceSnapshot>();
        var checkingBalance = checking.OpeningBalance;
        var cardBalance = card.OpeningBalance;

        for (var i = 0; i < Months; i++)
        {
            var month = start.AddMonths(i);
            var payDate = new DateTimeOffset(month.Year, month.Month, 1, 0, 0, 0, TimeSpan.Zero);

            // Salary grows ~3%/yr; bonus in December. Income lands in checking.
            long paycheck = 520_000 + i / 12 * 16_000;
            checkingBalance += AddTx(transactions, checking, userId, payDate.AddDays(1), paycheck, Cat("Paychecks"), "Acme Corp");
            if (month.Month == 12)
            {
                checkingBalance += AddTx(transactions, checking, userId, payDate.AddDays(15), 800_000, Cat("Paychecks"), "Acme Corp Bonus");
            }
            if (month.Month % 3 == 0)
            {
                checkingBalance += AddTx(transactions, brokerage, userId, payDate.AddDays(20), 25_000 + i * 400, Cat("Dividends & Capital Gains"), "Vanguard");
            }

            foreach (var e in expenses)
            {
                var onCard = e.Merchant is "Olive Garden" or "Starbucks" or "Amazon";
                for (var n = 0; n < e.Count; n++)
                {
                    long amt = -(e.Min + (long)(rng.NextDouble() * (e.Max - e.Min)));
                    var date = payDate.AddDays(2 + rng.Next(0, 25));
                    if (onCard)
                    {
                        cardBalance += AddTx(transactions, card, userId, date, amt, Cat(e.Category), e.Merchant);
                    }
                    else
                    {
                        checkingBalance += AddTx(transactions, checking, userId, date, amt, Cat(e.Category), e.Merchant);
                    }
                }
            }

            // Month-end balances grow steadily so net worth looks regularly tracked.
            var asOf = month.AddMonths(1).AddDays(-1);
            snapshots.Add(Snap(userId, checking, asOf, checkingBalance));
            snapshots.Add(Snap(userId, card, asOf, cardBalance));
            snapshots.Add(Snap(userId, savings, asOf, savings.OpeningBalance + (i + 1) * 80_000));
            snapshots.Add(Snap(userId, brokerage, asOf, (long)(brokerage.OpeningBalance * Math.Pow(1.012, i + 1))));
            snapshots.Add(Snap(userId, mortgage, asOf, mortgage.OpeningBalance + (i + 1) * 75_000));
        }
#pragma warning restore CA5394

        context.Transactions.AddRange(transactions);
        context.AccountBalanceSnapshots.AddRange(snapshots);
        await context.SaveChangesAsync(cancellationToken);

        await SeedSpendingPlansAsync(context, userId, Cat, cancellationToken);

        logger.LogInformation(
            "Seeded LLM test user with {AccountCount} accounts, {TxCount} transactions, {SnapCount} balance snapshots.",
            5,
            transactions.Count,
            snapshots.Count);
    }

    private static long AddTx(List<Transaction> list, Account account, int userId, DateTimeOffset date, long amount, int categoryId, string merchant)
    {
        list.Add(new Transaction
        {
            UserId = userId,
            Account = account,
            Date = date,
            Amount = amount,
            CategoryId = categoryId,
            Merchant = merchant,
            Source = TransactionSource.Import
        });
        return amount;
    }

    private static Account NewAccount(int userId, string name, AccountType type, AccountClass cls, long opening, DateOnly start)
    {
        var created = new DateTimeOffset(start.Year, start.Month, 1, 0, 0, 0, TimeSpan.Zero);
        return new Account
        {
            UserId = userId,
            Name = name,
            Type = type,
            Class = cls,
            Currency = Currency,
            OpeningBalance = opening,
            CreatedAt = created,
            UpdatedAt = created
        };
    }

    private static AccountBalanceSnapshot Snap(int userId, Account account, DateOnly asOf, long balance) => new()
    {
        UserId = userId,
        Account = account,
        AsOf = asOf,
        Balance = balance
    };

    private static async Task SeedSpendingPlansAsync(DataContext context, int userId, Func<string, int> cat, CancellationToken cancellationToken)
    {
        var monthly = new SpendingPlan
        {
            UserId = userId,
            Name = "Monthly Budget",
            Description = "Day-to-day spending plan",
            Currency = Currency,
            Incomes =
            [
                new SpendingPlanIncome { UserId = userId, Name = "Salary", Amount = 6_240_000, Cadence = SpendingPlanCadence.Annual, CategoryId = cat("Paychecks") }
            ],
            Expenses =
            [
                new SpendingPlanExpense { UserId = userId, Name = "Mortgage", Amount = 180_000, Cadence = SpendingPlanCadence.Monthly, CategoryId = cat("Mortgage") },
                new SpendingPlanExpense { UserId = userId, Name = "Groceries", Amount = 100_000, Cadence = SpendingPlanCadence.Monthly, CategoryId = cat("Groceries") },
                new SpendingPlanExpense { UserId = userId, Name = "Gas", Amount = 24_000, Cadence = SpendingPlanCadence.Monthly, CategoryId = cat("Gas") },
                new SpendingPlanExpense { UserId = userId, Name = "Utilities", Amount = 16_000, Cadence = SpendingPlanCadence.Monthly, CategoryId = cat("Gas & Electric") },
                new SpendingPlanExpense { UserId = userId, Name = "Dining out", Amount = 30_000, Cadence = SpendingPlanCadence.Monthly, CategoryId = cat("Restaurants & Bars") }
            ]
        };

        var travel = new SpendingPlan
        {
            UserId = userId,
            Name = "Travel Fund",
            Description = "Annual travel savings",
            Currency = Currency,
            Expenses =
            [
                new SpendingPlanExpense { UserId = userId, Name = "Summer trip", Amount = 400_000, Cadence = SpendingPlanCadence.Annual, CategoryId = cat("Travel & Vacation") }
            ]
        };

        context.SpendingPlans.AddRange(monthly, travel);
        await context.SaveChangesAsync(cancellationToken);
    }
}
