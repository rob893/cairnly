using System.Collections.Generic;
using Cairnly.API.Models.Entities;

namespace Cairnly.API.Data.Seeding;

/// <summary>A system category group (parent) and its child categories.</summary>
internal sealed record CategoryGroup(string Group, CategoryKind Kind, IReadOnlyList<(string Name, string Icon)> Children);

/// <summary>An admin spending-plan income line item (all annual figures).</summary>
internal sealed record IncomeSeed(string Name, string Category, long Amount);

/// <summary>An admin spending-plan expense line item in integer minor units.</summary>
internal sealed record ExpenseSeed(string Name, string? Description, long Amount, SpendingPlanCadence Cadence, string Category, bool Essential);

/// <summary>
/// Static seed data definitions (system categories and the admin spending plan) used by
/// <see cref="DatabaseSeeder"/>. Kept separate from the seeder logic so the data can grow without
/// bloating the seeder.
/// </summary>
internal static class SeedData
{
    /// <summary>
    /// System category groups (parent categories) and their child categories. The parent group has
    /// no icon; children carry an emoji. Each child inherits its group's kind.
    /// </summary>
    public static readonly IReadOnlyList<CategoryGroup> CategoryGroups =
    [
        new("Income", CategoryKind.Income,
        [
            ("Paychecks", "💵"),
            ("Interest", "💸"),
            ("Business Income", "💰"),
            ("Other Income", "💰"),
            ("Dividends & Capital Gains", "📈")
        ]),
        new("Gifts & Donations", CategoryKind.Expense,
        [
            ("Charity", "🎗"),
            ("Gifts", "🎁")
        ]),
        new("Auto & Transport", CategoryKind.Expense,
        [
            ("Auto Payment", "🚗"),
            ("Public Transit", "🚃"),
            ("Gas", "⛽️"),
            ("Auto Maintenance", "🔧"),
            ("Parking & Tolls", "🏢"),
            ("Taxi & Ride Shares", "🚕")
        ]),
        new("Housing", CategoryKind.Expense,
        [
            ("Mortgage", "🏠"),
            ("Rent", "🏠"),
            ("Home Improvement", "🔨")
        ]),
        new("Bills & Utilities", CategoryKind.Expense,
        [
            ("Garbage", "🗑"),
            ("Water", "💧"),
            ("Gas & Electric", "⚡️"),
            ("Internet & Cable", "🌐"),
            ("Phone", "📱")
        ]),
        new("Food & Dining", CategoryKind.Expense,
        [
            ("Groceries", "🍏"),
            ("Restaurants & Bars", "🍽"),
            ("Coffee Shops", "☕️")
        ]),
        new("Travel & Lifestyle", CategoryKind.Expense,
        [
            ("Travel & Vacation", "🏝"),
            ("Entertainment & Recreation", "🎥"),
            ("Personal", "👑"),
            ("Pets", "🐶"),
            ("Fun Money", "😜")
        ]),
        new("Shopping", CategoryKind.Expense,
        [
            ("Shopping", "🛍"),
            ("Clothing", "👕"),
            ("Furniture & Housewares", "🪑"),
            ("Electronics", "🖥")
        ]),
        new("Children", CategoryKind.Expense,
        [
            ("Child Care", "👶"),
            ("Child Activities", "⚽️")
        ]),
        new("Education", CategoryKind.Expense,
        [
            ("Student Loans", "🎓"),
            ("Education", "🏫")
        ]),
        new("Health & Wellness", CategoryKind.Expense,
        [
            ("Medical", "💊"),
            ("Dentist", "🦷"),
            ("Fitness", "💪")
        ]),
        new("Financial", CategoryKind.Expense,
        [
            ("Loan Repayment", "💰"),
            ("Financial & Legal Services", "🗄"),
            ("Financial Fees", "🏦"),
            ("Cash & ATM", "🏧"),
            ("Insurance", "☂️"),
            ("Taxes", "🏛️")
        ]),
        new("Other", CategoryKind.Expense,
        [
            ("Uncategorized", "❓"),
            ("Check", "💸"),
            ("Miscellaneous", "💲")
        ]),
        new("Business", CategoryKind.Expense,
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
        new("Transfers", CategoryKind.Transfer,
        [
            ("Transfer", "🔁"),
            ("Credit Card Payment", "💳"),
            ("Balance Adjustments", "⚖️"),
            ("Buy", "➡️"),
            ("Sell", "⬅️")
        ])
    ];

    /// <summary>Admin spending-plan income line items (all annual figures).</summary>
    public static readonly IReadOnlyList<IncomeSeed> AdminIncomes =
    [
        new("Interest and Dividends", "Dividends & Capital Gains", 300_000),
        new("Microsoft W2", "Paychecks", 19_000_000),
        new("Microsoft Perks+", "Other Income", 150_000),
        new("Microsoft Bonus", "Paychecks", 3_800_000),
        new("Microsoft Stock", "Paychecks", 6_000_000)
    ];

    /// <summary>Admin spending-plan expense line items in integer minor units.</summary>
    public static readonly IReadOnlyList<ExpenseSeed> AdminExpenses =
    [
        new("Taxable Brokerage", "Robinhood Account", 50_000, SpendingPlanCadence.Weekly, "Financial & Legal Services", false),
        new("Mortgage, HOA, Utilities", "3026.83 for Mortgage/tax/insurance\n150 HOA\n~500 for utilities (power, gas, water, trash)", 400_000, SpendingPlanCadence.Monthly, "Mortgage", true),
        new("Netflix", null, 500, SpendingPlanCadence.Monthly, "Entertainment & Recreation", false),
        new("Disney+", null, 500, SpendingPlanCadence.Monthly, "Entertainment & Recreation", false),
        new("Haircut", null, 5_000, SpendingPlanCadence.Monthly, "Personal", false),
        new("Hulu", null, 500, SpendingPlanCadence.Monthly, "Entertainment & Recreation", false),
        new("Internet", null, 7_000, SpendingPlanCadence.Monthly, "Internet & Cable", true),
        new("Phone", null, 9_500, SpendingPlanCadence.Monthly, "Phone", true),
        new("WoW", null, 1_500, SpendingPlanCadence.Monthly, "Entertainment & Recreation", false),
        new("Amazon Prime", null, 14_000, SpendingPlanCadence.Annual, "Shopping", false),
        new("Car Registration", "GA is super cheap lol", 2_000, SpendingPlanCadence.Annual, "Auto Maintenance", true),
        new("Domain Names", "rwherber.com and knowones.com and derpcode and tidygal\nManaged through godaddy", 8_000, SpendingPlanCadence.Annual, "Miscellaneous", false),
        new("Web Server Hosting", "Digital ocean VM", 2_900, SpendingPlanCadence.Monthly, "Miscellaneous", false),
        new("YouTube Premium", null, 1_000, SpendingPlanCadence.Monthly, "Entertainment & Recreation", false),
        new("Food (Groceries/Going Out)", null, 40_000, SpendingPlanCadence.Monthly, "Groceries", true),
        new("Gas", null, 7_500, SpendingPlanCadence.Monthly, "Gas", true),
        new("Car Insurance", null, 15_300, SpendingPlanCadence.Monthly, "Insurance", true),
        new("401k", null, 2_300_000, SpendingPlanCadence.Annual, "Financial & Legal Services", true),
        new("Federal Tax", "From 2024 tax return", 7_520_600, SpendingPlanCadence.Annual, "Taxes", true),
        new("GA State Tax", "From 2024 tax return", 1_200_000, SpendingPlanCadence.Annual, "Taxes", true),
        new("Amazon Fresh Membership", "Fresh Membership for free deliveries", 1_000, SpendingPlanCadence.Monthly, "Groceries", false),
        new("Pet Insurance", null, 5_600, SpendingPlanCadence.Monthly, "Pets", false),
        new("HBO Max", null, 500, SpendingPlanCadence.Monthly, "Entertainment & Recreation", false),
        new("Pest Control", null, 35_200, SpendingPlanCadence.Annual, "Home Improvement", true),
        new("Misc Spending", null, 15_000, SpendingPlanCadence.Weekly, "Miscellaneous", false),
        new("Mortgage Additional Principal Payments", null, 100_000, SpendingPlanCadence.Monthly, "Mortgage", false),
        new("House cleaning", null, 25_000, SpendingPlanCadence.Monthly, "Home Improvement", false),
        new("Termite Bond", null, 35_000, SpendingPlanCadence.Annual, "Home Improvement", true),
        new("OpenAI Sub", null, 2_000, SpendingPlanCadence.Monthly, "Miscellaneous", false),
        new("HSA", null, 440_000, SpendingPlanCadence.Annual, "Medical", true),
        new("Umbrella Insurance", null, 33_000, SpendingPlanCadence.Annual, "Insurance", true)
    ];
}
