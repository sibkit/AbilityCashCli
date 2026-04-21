using System;
using System.Collections.Generic;

namespace AbilityCashCli.Data.Entities;

public partial class DataSeriesCategoryFilter
{
    public int Id { get; set; }

    public byte[] Guid { get; set; } = null!;

    public int Changed { get; set; }

    public int Deleted { get; set; }

    public int DataSeries { get; set; }

    public int Category { get; set; }

    public virtual Category CategoryNavigation { get; set; } = null!;

    public virtual DataSeries DataSeriesNavigation { get; set; } = null!;
}
