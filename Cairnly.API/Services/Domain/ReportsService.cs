using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Data.Repositories;
using Cairnly.API.Models.Dtos;
using Cairnly.API.Models.Entities;
using Cairnly.API.Models.QueryParameters;
using Cairnly.API.Models.Reports;
using Cairnly.API.Services.Auth;

namespace Cairnly.API.Services.Domain;

/// <summary>
/// Builds transaction-backed financial reports for the current user.
/// </summary>
public sealed class ReportsService : IReportsService
{
    private const string DefaultCurrency = "USD";

    /// <summary>The maximum number of breakdown rows before the remainder is folded into "Other".</summary>
    private const int MaxBreakdownRows = 8;

    private readonly ITransactionRepository transactionRepository;

    private readonly IAccountRepository accountRepository;

    private readonly ICurrentUserService currentUserService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ReportsService"/> class.
    /// </summary>
    /// <param name="transactionRepository">The transaction repository.</param>
    /// <param name="accountRepository">The account repository.</param>
    /// <param name="currentUserService">The current user service.</param>
    public ReportsService(
        ITransactionRepository transactionRepository,
        IAccountRepository accountRepository,
        ICurrentUserService currentUserService)
    {
        this.transactionRepository = transactionRepository ?? throw new ArgumentNullException(nameof(transactionRepository));
        this.accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        this.currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
    }

    /// <inheritdoc />
    public async Task<CashFlowReportDto> GetCashFlowAsync(CashFlowReportQueryParameters queryParameters, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(queryParameters);

        var userId = this.currentUserService.UserId;
        var from = WindowStart(queryParameters.Timeframe, DateTimeOffset.UtcNow);

        var entries = await this.transactionRepository.GetCashFlowEntriesAsync(userId, from, cancellationToken);
        var accounts = await this.accountRepository.SearchAsync(a => a.UserId == userId, track: false, cancellationToken);
        var currency = accounts.Count > 0 ? accounts[0].Currency : DefaultCurrency;

        var series = entries
            .GroupBy(e => PeriodStart(e.Date, queryParameters.Period))
            .OrderBy(g => g.Key)
            .Select(g => BuildPoint(g.Key, queryParameters.Period, g.ToList()))
            .ToList();

        long income = series.Sum(p => p.Income);
        long expenses = series.Sum(p => p.Expenses);
        long savings = income - expenses;
        double savingsRate = income > 0 ? (double)savings / income * 100d : 0d;

        return new CashFlowReportDto
        {
            Currency = currency,
            Period = queryParameters.Period,
            Summary = new CashFlowSummaryDto
            {
                Income = income,
                Expenses = expenses,
                Savings = savings,
                SavingsRate = savingsRate
            },
            Series = series
        };
    }

    /// <summary>Builds one period point with income/expense totals and breakdowns.</summary>
    private static CashFlowPointDto BuildPoint(DateOnly periodStart, CashFlowPeriod period, IReadOnlyList<CashFlowEntry> entries)
    {
        var incomeEntries = entries.Where(e => e.Kind == CategoryKind.Income).ToList();
        var expenseEntries = entries.Where(e => e.Kind == CategoryKind.Expense).ToList();

        long income = incomeEntries.Sum(e => Math.Abs(e.Amount));
        long expenses = expenseEntries.Sum(e => Math.Abs(e.Amount));

        return new CashFlowPointDto
        {
            PeriodStart = periodStart,
            Label = PeriodLabel(periodStart, period),
            Income = income,
            Expenses = expenses,
            Net = income - expenses,
            IncomeBreakdowns = BuildBreakdowns(incomeEntries, income),
            ExpenseBreakdowns = BuildBreakdowns(expenseEntries, expenses)
        };
    }

    /// <summary>Builds the category/group/merchant breakdowns for one section.</summary>
    private static CashFlowBreakdownsDto BuildBreakdowns(IReadOnlyList<CashFlowEntry> entries, long total)
    {
        return new CashFlowBreakdownsDto
        {
            ByCategory = TopWithOther(entries
                .GroupBy(e => e.CategoryId)
                .Select(g => new CashFlowBreakdownItemDto
                {
                    Key = g.Key.ToString(CultureInfo.InvariantCulture),
                    Label = g.First().CategoryName,
                    Icon = g.First().CategoryIcon,
                    Amount = g.Sum(e => Math.Abs(e.Amount)),
                    Percent = Share(g.Sum(e => Math.Abs(e.Amount)), total)
                }), total),
            ByGroup = TopWithOther(entries
                .GroupBy(e => e.ParentCategoryId ?? e.CategoryId)
                .Select(g => new CashFlowBreakdownItemDto
                {
                    Key = g.Key.ToString(CultureInfo.InvariantCulture),
                    Label = g.First().ParentName ?? g.First().CategoryName,
                    Icon = g.First().ParentIcon ?? g.First().CategoryIcon,
                    Amount = g.Sum(e => Math.Abs(e.Amount)),
                    Percent = Share(g.Sum(e => Math.Abs(e.Amount)), total)
                }), total),
            ByMerchant = TopWithOther(entries
                .GroupBy(e => e.Merchant)
                .Select(g => new CashFlowBreakdownItemDto
                {
                    Key = g.Key ?? string.Empty,
                    Label = string.IsNullOrWhiteSpace(g.Key) ? "No merchant" : g.Key!,
                    Icon = null,
                    Amount = g.Sum(e => Math.Abs(e.Amount)),
                    Percent = Share(g.Sum(e => Math.Abs(e.Amount)), total)
                }), total)
        };
    }

    /// <summary>Sorts rows by amount, keeps the top N, and folds the rest into an "Other" row.</summary>
    private static List<CashFlowBreakdownItemDto> TopWithOther(IEnumerable<CashFlowBreakdownItemDto> items, long total)
    {
        var ordered = items.OrderByDescending(i => i.Amount).ToList();

        if (ordered.Count <= MaxBreakdownRows)
        {
            return ordered;
        }

        var top = ordered.Take(MaxBreakdownRows).ToList();
        long otherAmount = ordered.Skip(MaxBreakdownRows).Sum(i => i.Amount);

        top.Add(new CashFlowBreakdownItemDto
        {
            Key = string.Empty,
            Label = "Other",
            Icon = null,
            Amount = otherAmount,
            Percent = Share(otherAmount, total)
        });

        return top;
    }

    /// <summary>Returns the share of an amount against a total as a percentage (0 when total is 0).</summary>
    private static double Share(long amount, long total) => total > 0 ? (double)amount / total * 100d : 0d;

    /// <summary>Computes the first day of the period bucket the given instant falls in (UTC).</summary>
    private static DateOnly PeriodStart(DateTimeOffset date, CashFlowPeriod period)
    {
        var utc = DateOnly.FromDateTime(date.UtcDateTime);

        return period switch
        {
            CashFlowPeriod.Quarterly => new DateOnly(utc.Year, (utc.Month - 1) / 3 * 3 + 1, 1),
            CashFlowPeriod.Yearly => new DateOnly(utc.Year, 1, 1),
            _ => new DateOnly(utc.Year, utc.Month, 1)
        };
    }

    /// <summary>Builds the human-readable label for a period bucket.</summary>
    private static string PeriodLabel(DateOnly start, CashFlowPeriod period)
    {
        return period switch
        {
            CashFlowPeriod.Quarterly => $"Q{(start.Month - 1) / 3 + 1} {start.Year}",
            CashFlowPeriod.Yearly => start.Year.ToString(CultureInfo.InvariantCulture),
            _ => start.ToString("MMM yyyy", CultureInfo.InvariantCulture)
        };
    }

    /// <summary>Maps the trailing timeframe to an inclusive window start, or null for all history.</summary>
    private static DateTimeOffset? WindowStart(BalanceHistoryTimeframe timeframe, DateTimeOffset now)
    {
        return timeframe switch
        {
            BalanceHistoryTimeframe.OneMonth => now.AddMonths(-1),
            BalanceHistoryTimeframe.ThreeMonths => now.AddMonths(-3),
            BalanceHistoryTimeframe.OneYear => now.AddYears(-1),
            BalanceHistoryTimeframe.All => null,
            _ => now.AddYears(-1)
        };
    }
}