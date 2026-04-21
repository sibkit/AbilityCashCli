using System;
using System.Collections.Generic;

namespace AbilityCashCli.Data.Entities;

public partial class Account
{
    public int Id { get; set; }

    public byte[] Guid { get; set; } = null!;

    public int Changed { get; set; }

    public bool Deleted { get; set; }

    public string Name { get; set; } = null!;

    public long StartingBalance { get; set; }

    public int Currency { get; set; }

    public string Comment { get; set; } = null!;

    public bool Locked { get; set; }

    public virtual ICollection<AccountLayout> AccountLayouts { get; set; } = new List<AccountLayout>();

    public virtual Currency CurrencyNavigation { get; set; } = null!;

    public virtual ICollection<DataSeriesAccountFilter> DataSeriesAccountFilters { get; set; } = new List<DataSeriesAccountFilter>();

    public virtual ICollection<Transaction> TransactionExpenseAccountNavigations { get; set; } = new List<Transaction>();

    public virtual ICollection<Transaction> TransactionIncomeAccountNavigations { get; set; } = new List<Transaction>();
}
