using System;
using System.Collections.Generic;

namespace AbilityCashCli.Data.Entities;

public partial class CurrencyRate
{
    public int Id { get; set; }

    public byte[] Guid { get; set; } = null!;

    public int Changed { get; set; }

    public int Deleted { get; set; }

    public int RateDate { get; set; }

    public int Currency1 { get; set; }

    public int Currency2 { get; set; }

    public int Value1 { get; set; }

    public int Value2 { get; set; }

    public virtual Currency Currency1Navigation { get; set; } = null!;

    public virtual Currency Currency2Navigation { get; set; } = null!;
}
