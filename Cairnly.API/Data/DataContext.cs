using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cairnly.API.Models;
using Cairnly.API.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Cairnly.API.Data;

/// <summary>
/// The main Entity Framework database context for Cairnly.
/// </summary>
public sealed class DataContext : IdentityDbContext<User, Role, int,
    IdentityUserClaim<int>, UserRole, IdentityUserLogin<int>,
    IdentityRoleClaim<int>, IdentityUserToken<int>>
{
    private static readonly ValueComparer<Dictionary<string, object>> metadataComparer = new(
        (left, right) => JsonSerializer.Serialize(left, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(right, (JsonSerializerOptions?)null),
        value => StringComparer.Ordinal.GetHashCode(JsonSerializer.Serialize(value, (JsonSerializerOptions?)null)),
        value => JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(value, (JsonSerializerOptions?)null), (JsonSerializerOptions?)null) ?? new Dictionary<string, object>());

    private static readonly ValueComparer<UserPreferencesData> userPreferencesComparer = new(
        (left, right) => left == right,
        value => value == null ? 0 : value.GetHashCode(),
        value => value);

    /// <summary>
    /// Initializes a new instance of the <see cref="DataContext"/> class.
    /// </summary>
    /// <param name="options">The database context options.</param>
    public DataContext(DbContextOptions<DataContext> options) : base(options) { }

    /// <summary>Gets the refresh tokens DbSet.</summary>
    public DbSet<RefreshToken> RefreshTokens => this.Set<RefreshToken>();

    /// <summary>Gets the linked accounts DbSet.</summary>
    public DbSet<LinkedAccount> LinkedAccounts => this.Set<LinkedAccount>();

    /// <summary>Gets the financial accounts DbSet.</summary>
    public DbSet<Account> Accounts => this.Set<Account>();

    /// <summary>Gets the categories DbSet.</summary>
    public DbSet<Category> Categories => this.Set<Category>();

    /// <summary>Gets the tags DbSet.</summary>
    public DbSet<Tag> Tags => this.Set<Tag>();

    /// <summary>Gets the transactions DbSet.</summary>
    public DbSet<Transaction> Transactions => this.Set<Transaction>();

    /// <summary>Gets the budgets DbSet.</summary>
    public DbSet<Budget> Budgets => this.Set<Budget>();

    /// <summary>Gets the budget income line items DbSet.</summary>
    public DbSet<BudgetIncome> BudgetIncomes => this.Set<BudgetIncome>();

    /// <summary>Gets the budget expense line items DbSet.</summary>
    public DbSet<BudgetExpense> BudgetExpenses => this.Set<BudgetExpense>();

    /// <summary>Gets the transaction-tag join DbSet.</summary>
    public DbSet<TransactionTag> TransactionTags => this.Set<TransactionTag>();

    /// <summary>Gets the user preferences DbSet.</summary>
    public DbSet<UserPreferences> UserPreferences => this.Set<UserPreferences>();

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        this.StampAuditableEntities();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        this.StampAuditableEntities();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    /// <summary>
    /// Stamps <see cref="IAuditableEntity.CreatedAt"/> and <see cref="IAuditableEntity.UpdatedAt"/>
    /// on added and modified entities so audit timestamps are maintained in one place.
    /// </summary>
    private void StampAuditableEntities()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in this.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        base.OnModelCreating(builder);

        builder.Entity<UserRole>(userRole =>
        {
            userRole.HasKey(ur => new { ur.UserId, ur.RoleId });

            userRole.HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId)
                .IsRequired();

            userRole.HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId)
                .IsRequired();
        });

        builder.Entity<RefreshToken>(rToken =>
        {
            rToken.HasKey(k => new { k.UserId, k.DeviceId });
            rToken.HasIndex(rt => rt.DeviceId);
        });

        builder.Entity<LinkedAccount>(linkedAccount =>
        {
            linkedAccount.HasKey(account => new { account.Id, account.LinkedAccountType });
            linkedAccount.Property(account => account.LinkedAccountType).HasConversion<string>();
        });

        builder.Entity<Account>(account =>
        {
            account.Property(a => a.Type).HasConversion<string>().HasMaxLength(32);
            account.Property(a => a.Class).HasConversion<string>().HasMaxLength(16);
            account.Property(a => a.Currency).HasMaxLength(3);
            account.Property(a => a.Metadata).HasColumnType("jsonb").Metadata.SetValueComparer(metadataComparer);
            account.Property(a => a.CreatedAt).HasDefaultValueSql("now()");
            account.Property(a => a.UpdatedAt).HasDefaultValueSql("now()");

            account.HasIndex(a => a.UserId);

            account.HasOne(a => a.User)
                .WithMany(u => u.Accounts)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Category>(category =>
        {
            category.Property(c => c.Kind).HasConversion<string>().HasMaxLength(16);
            category.Property(c => c.Metadata).HasColumnType("jsonb").Metadata.SetValueComparer(metadataComparer);
            category.Property(c => c.CreatedAt).HasDefaultValueSql("now()");
            category.Property(c => c.UpdatedAt).HasDefaultValueSql("now()");

            category.HasIndex(c => c.UserId);
            category.HasIndex(c => c.IsSystem);

            category.HasOne(c => c.User)
                .WithMany(u => u.Categories)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            category.HasOne(c => c.Parent)
                .WithMany(c => c.Children)
                .HasForeignKey(c => c.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Tag>(tag =>
        {
            tag.Property(t => t.Name).HasMaxLength(100);
            tag.Property(t => t.Metadata).HasColumnType("jsonb").Metadata.SetValueComparer(metadataComparer);
            tag.Property(t => t.CreatedAt).HasDefaultValueSql("now()");
            tag.Property(t => t.UpdatedAt).HasDefaultValueSql("now()");

            tag.HasIndex(t => new { t.UserId, t.Name }).IsUnique();

            tag.HasOne(t => t.User)
                .WithMany(u => u.Tags)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Transaction>(transaction =>
        {
            transaction.Property(t => t.Source).HasConversion<string>().HasMaxLength(16);
            transaction.Property(t => t.Merchant).HasMaxLength(255);
            transaction.Property(t => t.Description).HasMaxLength(1024);
            transaction.Property(t => t.Metadata).HasColumnType("jsonb").Metadata.SetValueComparer(metadataComparer);
            transaction.Property(t => t.CreatedAt).HasDefaultValueSql("now()");
            transaction.Property(t => t.UpdatedAt).HasDefaultValueSql("now()");

            transaction.HasIndex(t => new { t.UserId, t.AccountId });
            transaction.HasIndex(t => new { t.UserId, t.Date });
            transaction.HasIndex(t => new { t.UserId, t.CategoryId });
            transaction.HasIndex(t => t.ParentTransactionId);

            transaction.HasOne(t => t.User)
                .WithMany(u => u.Transactions)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            transaction.HasOne(t => t.Account)
                .WithMany(a => a.Transactions)
                .HasForeignKey(t => t.AccountId)
                .OnDelete(DeleteBehavior.Cascade);

            transaction.HasOne(t => t.Category)
                .WithMany(c => c.Transactions)
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            transaction.HasOne(t => t.ParentTransaction)
                .WithMany(t => t.Splits)
                .HasForeignKey(t => t.ParentTransactionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TransactionTag>(transactionTag =>
        {
            transactionTag.HasKey(tt => new { tt.TransactionId, tt.TagId });

            transactionTag.HasIndex(tt => tt.TagId);

            transactionTag.HasOne(tt => tt.Transaction)
                .WithMany(t => t.TransactionTags)
                .HasForeignKey(tt => tt.TransactionId)
                .OnDelete(DeleteBehavior.Cascade);

            transactionTag.HasOne(tt => tt.Tag)
                .WithMany(t => t.TransactionTags)
                .HasForeignKey(tt => tt.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Budget>(budget =>
        {
            budget.Property(b => b.Currency).HasMaxLength(3);
            budget.Property(b => b.Metadata).HasColumnType("jsonb").Metadata.SetValueComparer(metadataComparer);
            budget.Property(b => b.CreatedAt).HasDefaultValueSql("now()");
            budget.Property(b => b.UpdatedAt).HasDefaultValueSql("now()");

            budget.HasIndex(b => b.UserId);

            budget.HasOne(b => b.User)
                .WithMany(u => u.Budgets)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<BudgetIncome>(income =>
        {
            income.Property(i => i.Type).HasConversion<string>().HasMaxLength(32);
            income.Property(i => i.Cadence).HasConversion<string>().HasMaxLength(16);
            income.Property(i => i.Metadata).HasColumnType("jsonb").Metadata.SetValueComparer(metadataComparer);
            income.Property(i => i.CreatedAt).HasDefaultValueSql("now()");
            income.Property(i => i.UpdatedAt).HasDefaultValueSql("now()");

            income.HasIndex(i => new { i.UserId, i.BudgetId });
            income.HasIndex(i => i.CategoryId);

            income.HasOne(i => i.User)
                .WithMany()
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            income.HasOne(i => i.Budget)
                .WithMany(b => b.Incomes)
                .HasForeignKey(i => i.BudgetId)
                .OnDelete(DeleteBehavior.Cascade);

            income.HasOne(i => i.Category)
                .WithMany()
                .HasForeignKey(i => i.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<BudgetExpense>(expense =>
        {
            expense.Property(e => e.Cadence).HasConversion<string>().HasMaxLength(16);
            expense.Property(e => e.Metadata).HasColumnType("jsonb").Metadata.SetValueComparer(metadataComparer);
            expense.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
            expense.Property(e => e.UpdatedAt).HasDefaultValueSql("now()");

            expense.HasIndex(e => new { e.UserId, e.BudgetId });
            expense.HasIndex(e => e.CategoryId);

            expense.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            expense.HasOne(e => e.Budget)
                .WithMany(b => b.Expenses)
                .HasForeignKey(e => e.BudgetId)
                .OnDelete(DeleteBehavior.Cascade);

            expense.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<BudgetIncomeTag>(incomeTag =>
        {
            incomeTag.HasKey(it => new { it.BudgetIncomeId, it.TagId });

            incomeTag.HasIndex(it => it.TagId);

            incomeTag.HasOne(it => it.BudgetIncome)
                .WithMany(i => i.BudgetIncomeTags)
                .HasForeignKey(it => it.BudgetIncomeId)
                .OnDelete(DeleteBehavior.Cascade);

            incomeTag.HasOne(it => it.Tag)
                .WithMany(t => t.BudgetIncomeTags)
                .HasForeignKey(it => it.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<BudgetExpenseTag>(expenseTag =>
        {
            expenseTag.HasKey(et => new { et.BudgetExpenseId, et.TagId });

            expenseTag.HasIndex(et => et.TagId);

            expenseTag.HasOne(et => et.BudgetExpense)
                .WithMany(e => e.BudgetExpenseTags)
                .HasForeignKey(et => et.BudgetExpenseId)
                .OnDelete(DeleteBehavior.Cascade);

            expenseTag.HasOne(et => et.Tag)
                .WithMany(t => t.BudgetExpenseTags)
                .HasForeignKey(et => et.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<UserPreferences>(preferences =>
        {
            preferences.Property(p => p.Data).HasColumnType("jsonb").Metadata.SetValueComparer(userPreferencesComparer);
            preferences.Property(p => p.CreatedAt).HasDefaultValueSql("now()");
            preferences.Property(p => p.UpdatedAt).HasDefaultValueSql("now()");

            preferences.HasIndex(p => p.UserId).IsUnique();

            preferences.HasOne(p => p.User)
                .WithOne(u => u.Preferences)
                .HasForeignKey<UserPreferences>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
