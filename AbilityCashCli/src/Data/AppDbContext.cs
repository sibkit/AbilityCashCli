using System;
using System.Collections.Generic;
using AbilityCashCli.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace AbilityCashCli.Data;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<AccountFolder> AccountFolders { get; set; }

    public virtual DbSet<AccountLayout> AccountLayouts { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Classifier> Classifiers { get; set; }

    public virtual DbSet<Currency> Currencies { get; set; }

    public virtual DbSet<CurrencyRate> CurrencyRates { get; set; }

    public virtual DbSet<DataSeries> DataSeries { get; set; }

    public virtual DbSet<DataSeriesAccountFilter> DataSeriesAccountFilters { get; set; }

    public virtual DbSet<DataSeriesCategoryFilter> DataSeriesCategoryFilters { get; set; }

    public virtual DbSet<InterfacePage> InterfacePages { get; set; }

    public virtual DbSet<Property> Properties { get; set; }

    public virtual DbSet<Transaction> Transactions { get; set; }

    public virtual DbSet<TransactionCategory> TransactionCategories { get; set; }

    public virtual DbSet<TransactionGroup> TransactionGroups { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasIndex(e => e.Guid, "IX_Accounts_Guid").IsUnique();

            entity.Property(e => e.Changed).HasColumnType("UNIXTIME");
            entity.Property(e => e.Deleted).HasColumnType("BOOLEAN");
            entity.Property(e => e.Guid).HasColumnType("GUID");
            entity.Property(e => e.Locked).HasColumnType("BOOLEAN");
            entity.Property(e => e.StartingBalance).HasColumnType("MONEY");

            entity.HasOne(d => d.CurrencyNavigation).WithMany(p => p.Accounts)
                .HasForeignKey(d => d.Currency)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<AccountFolder>(entity =>
        {
            entity.HasIndex(e => e.Guid, "IX_AccountFolders_Guid").IsUnique();

            entity.HasOne(d => d.ParentNavigation).WithMany(p => p.InverseParentNavigation).HasForeignKey(d => d.Parent);
        });

        modelBuilder.Entity<AccountLayout>(entity =>
        {
            entity.HasIndex(e => e.Guid, "IX_AccountLayouts_Guid").IsUnique();

            entity.HasOne(d => d.AccountNavigation).WithMany(p => p.AccountLayouts)
                .HasForeignKey(d => d.Account)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.FolderNavigation).WithMany(p => p.AccountLayouts)
                .HasForeignKey(d => d.Folder)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(e => e.Guid, "IX_Categories_Guid").IsUnique();

            entity.HasOne(d => d.ParentNavigation).WithMany(p => p.InverseParentNavigation).HasForeignKey(d => d.Parent);
        });

        modelBuilder.Entity<Classifier>(entity =>
        {
            entity.HasIndex(e => e.Guid, "IX_Classifiers_Guid").IsUnique();

            entity.HasOne(d => d.ExpenseTreeRootNavigation).WithMany(p => p.ClassifierExpenseTreeRootNavigations).HasForeignKey(d => d.ExpenseTreeRoot);

            entity.HasOne(d => d.IncomeTreeRootNavigation).WithMany(p => p.ClassifierIncomeTreeRootNavigations).HasForeignKey(d => d.IncomeTreeRoot);

            entity.HasOne(d => d.TransferTreeRootNavigation).WithMany(p => p.ClassifierTransferTreeRootNavigations).HasForeignKey(d => d.TransferTreeRoot);
        });

        modelBuilder.Entity<Currency>(entity =>
        {
            entity.HasIndex(e => e.Guid, "IX_Currencies_Guid").IsUnique();
        });

        modelBuilder.Entity<CurrencyRate>(entity =>
        {
            entity.HasIndex(e => e.Guid, "IX_CurrencyRates_Guid").IsUnique();

            entity.HasOne(d => d.Currency1Navigation).WithMany(p => p.CurrencyRateCurrency1Navigations)
                .HasForeignKey(d => d.Currency1)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Currency2Navigation).WithMany(p => p.CurrencyRateCurrency2Navigations)
                .HasForeignKey(d => d.Currency2)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<DataSeries>(entity =>
        {
            entity.HasIndex(e => e.Guid, "IX_DataSeries_Guid").IsUnique();
        });

        modelBuilder.Entity<DataSeriesAccountFilter>(entity =>
        {
            entity.HasIndex(e => e.Guid, "IX_DataSeriesAccountFilters_Guid").IsUnique();

            entity.HasOne(d => d.AccountNavigation).WithMany(p => p.DataSeriesAccountFilters)
                .HasForeignKey(d => d.Account)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.DataSeriesNavigation).WithMany(p => p.DataSeriesAccountFilters)
                .HasForeignKey(d => d.DataSeries)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<DataSeriesCategoryFilter>(entity =>
        {
            entity.HasIndex(e => e.Guid, "IX_DataSeriesCategoryFilters_Guid").IsUnique();

            entity.HasOne(d => d.CategoryNavigation).WithMany(p => p.DataSeriesCategoryFilters)
                .HasForeignKey(d => d.Category)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.DataSeriesNavigation).WithMany(p => p.DataSeriesCategoryFilters)
                .HasForeignKey(d => d.DataSeries)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<InterfacePage>(entity =>
        {
            entity.HasIndex(e => e.Guid, "IX_InterfacePages_Guid").IsUnique();

            entity.HasOne(d => d.ClassifierNavigation).WithMany(p => p.InterfacePages).HasForeignKey(d => d.Classifier);

            entity.HasOne(d => d.OwnerNavigation).WithMany(p => p.InterfacePages)
                .HasForeignKey(d => d.Owner)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<Property>(entity =>
        {
            entity.HasNoKey();

            entity.HasIndex(e => e.Key, "IX_Properties_Key").IsUnique();
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasIndex(e => e.Guid, "IX_Transactions_Guid").IsUnique();

            entity.HasOne(d => d.ExpenseAccountNavigation).WithMany(p => p.TransactionExpenseAccountNavigations).HasForeignKey(d => d.ExpenseAccount);

            entity.HasOne(d => d.GroupNavigation).WithMany(p => p.Transactions)
                .HasForeignKey(d => d.Group)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.IncomeAccountNavigation).WithMany(p => p.TransactionIncomeAccountNavigations).HasForeignKey(d => d.IncomeAccount);
        });

        modelBuilder.Entity<TransactionCategory>(entity =>
        {
            entity.HasIndex(e => e.Guid, "IX_TransactionCategories_Guid").IsUnique();

            entity.HasOne(d => d.CategoryNavigation).WithMany(p => p.TransactionCategories)
                .HasForeignKey(d => d.Category)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.TransactionNavigation).WithMany(p => p.TransactionCategories)
                .HasForeignKey(d => d.Transaction)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<TransactionGroup>(entity =>
        {
            entity.HasIndex(e => e.Guid, "IX_TransactionGroups_Guid").IsUnique();
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Guid, "IX_Users_Guid").IsUnique();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
