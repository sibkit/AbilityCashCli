using System;
using System.Collections.Generic;

namespace AbilityCashCli.Data.Entities;

public partial class DataSeries
{
    public int Id { get; set; }

    public byte[] Guid { get; set; } = null!;

    public int Changed { get; set; }

    public int Deleted { get; set; }

    public string DataSeriesName { get; set; } = null!;

    public int ColorHue { get; set; }

    public int Income { get; set; }

    public int Expense { get; set; }

    public int TransfersReceived { get; set; }

    public int TransfersSent { get; set; }

    public int Absolute { get; set; }

    public int HideBars { get; set; }

    public int HideLine { get; set; }

    public virtual ICollection<DataSeriesAccountFilter> DataSeriesAccountFilters { get; set; } = new List<DataSeriesAccountFilter>();

    public virtual ICollection<DataSeriesCategoryFilter> DataSeriesCategoryFilters { get; set; } = new List<DataSeriesCategoryFilter>();
}
