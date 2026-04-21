using System;
using System.Collections.Generic;

namespace AbilityCashCli.Data.Entities;

public partial class Currency
{
    public int Id { get; set; }

    public byte[] Guid { get; set; } = null!;

    public int Changed { get; set; }

    public int Deleted { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int Precision { get; set; }

    public virtual ICollection<Account> Accounts { get; set; } = new List<Account>();

    public virtual ICollection<CurrencyRate> CurrencyRateCurrency1Navigations { get; set; } = new List<CurrencyRate>();

    public virtual ICollection<CurrencyRate> CurrencyRateCurrency2Navigations { get; set; } = new List<CurrencyRate>();
}
