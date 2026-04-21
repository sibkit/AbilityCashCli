using System;
using System.Collections.Generic;

namespace AbilityCashCli.Data.Entities;

public partial class Transaction
{
    public int Id { get; set; }

    public byte[] Guid { get; set; } = null!;

    public int Changed { get; set; }

    public int Deleted { get; set; }

    public int Group { get; set; }

    public int Position { get; set; }

    public int BudgetDate { get; set; }

    public int Executed { get; set; }

    public int Locked { get; set; }

    public int? IncomeAccount { get; set; }

    public long? IncomeAmount { get; set; }

    public long? IncomeBalance { get; set; }

    public int? ExpenseAccount { get; set; }

    public long? ExpenseAmount { get; set; }

    public long? ExpenseBalance { get; set; }

    public int? Quantity { get; set; }

    public string Comment { get; set; } = null!;

    public string ExtraComment1 { get; set; } = null!;

    public string ExtraComment2 { get; set; } = null!;

    public string ExtraComment3 { get; set; } = null!;

    public string ExtraComment4 { get; set; } = null!;

    public int? BudgetPeriodEnd { get; set; }

    public virtual Account? ExpenseAccountNavigation { get; set; }

    public virtual TransactionGroup GroupNavigation { get; set; } = null!;

    public virtual Account? IncomeAccountNavigation { get; set; }

    public virtual ICollection<TransactionCategory> TransactionCategories { get; set; } = new List<TransactionCategory>();
}
